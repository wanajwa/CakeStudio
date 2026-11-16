
using CakeStudio.Constant;
using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.Resources;
using CakeStudio.Services;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace CakeStudio.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderController> _logger;
        private readonly Stripe.StripeClient _stripeClient;
        private readonly IConfiguration _configuration;
        public OrderController( IOrderService orderService,ICartService cartService, ILogger<OrderController> logger, Stripe.StripeClient stripeClient, IConfiguration configuration)
        {
            _orderService = orderService;
            _cartService = cartService;
            _logger = logger;
            _stripeClient = stripeClient;
            _configuration = configuration;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id
                : null;
        }

        private void RestoreOrderItemNames(OrderViewModel model)
        {
            var details = _orderService.GetOrderDetails(model.OrderId);

            if (details == null)
                return;

            foreach (var item in model.OrderItems)
            {
                var savedItem = details.OrderItems
                    .FirstOrDefault(x => x.OrderItemId == item.OrderItemId);

                // Restore display information; keep the submitted message
                item.Cake = savedItem?.Cake;
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Checkout()
        {
            return RedirectToAction("Index", "Cart");
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(List<CartItemViewModel> items, DateTime? requiredDate, TimeSpan? requiredTime, int? orderId, Guid checkoutRequestId)
        {
            var customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            return await RunCheckoutLockedAsync(customerId.Value, async () =>
            {

                if (!ModelState.IsValid || checkoutRequestId == Guid.Empty)
                {
                    TempData["ErrorMessage"] = Resource.InvalidCheckoutDetails;
                    return RedirectToAction("Index", "Cart");
                }

                if (!orderId.HasValue)
                {
                    var existingCheckoutOrderId = _orderService.FindCheckoutOrderId(
                        customerId.Value, checkoutRequestId);

                    if (existingCheckoutOrderId.HasValue)
                    {
                        return RedirectToAction(nameof(Details), new { id = existingCheckoutOrderId.Value });
                    }
                }

                var order = new OrderViewModel
                {
                    RequiredDate = requiredDate,
                    RequiredTime = requiredTime
                };

                HttpContext.Session.SetString($"CheckoutDraft_{customerId.Value}", JsonSerializer.Serialize(new { order.RequiredDate, order.RequiredTime }));

                if (items.Count == 0)
                {
                    TempData["ErrorMessage"] = Resource.CartEmpty;
                    return RedirectToAction("Index", "Cart");
                }

                try
                {
                    // Save the submitted options, quantities, and messages.
                    // UpdateItems also validates the options and calculates current prices.
                    _cartService.UpdateItems(items);
                    order.Cart = _cartService.GetCart();
                }
                catch (ArgumentException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction("Index", "Cart");
                }


                if (!requiredDate.HasValue || !requiredTime.HasValue || requiredTime.Value < TimeSpan.Zero || requiredTime.Value >= TimeSpan.FromDays(1) || requiredDate.Value.Date <= DateTime.Now)
                {
                    TempData["ErrorMessage"] = Resource.InvalidRequiredDateTime;
                    return RedirectToAction("Index", "Cart");
                }

                try
                {
                    if (orderId.HasValue)
                    {
                        var savedOrder = _orderService.GetOrderDetails(orderId.Value);

                        if (savedOrder == null)
                            return NotFound();

                        if (savedOrder.Order.CustId != customerId.Value)
                            return Forbid();

                        if (savedOrder.Order.OrderStatus != OrderStatus.PendingPayment)
                            return RedirectToAction(nameof(Details), new { id = orderId.Value });

                        var previousSessionId = savedOrder.Payment?.StripeCheckoutSessionId;

                        if (!string.IsNullOrEmpty(previousSessionId))
                        {
                            var stripeSessions = _stripeClient.V1.Checkout.Sessions;
                            var cancellationToken = HttpContext.RequestAborted;

                            var previousSession = await stripeSessions.GetAsync(previousSessionId, cancellationToken: cancellationToken);

                            if (previousSession.PaymentStatus == "paid")
                            {
                                return RedirectToAction(nameof(Payment), new { sessionId = previousSessionId });
                            }

                            if (previousSession.Status == "open")
                            {
                                previousSession = await stripeSessions.ExpireAsync(previousSessionId, cancellationToken: cancellationToken);
                            }

                            if (previousSession.Status != "expired" || previousSession.PaymentStatus != "unpaid")
                            {
                                TempData["ErrorMessage"] = Resource.PreviousPaymentProcessing;
                                return RedirectToAction(nameof(Details), new { id = orderId.Value });
                            }

                            _orderService.ClearStripeSession(orderId.Value, customerId.Value, previousSessionId);
                        }
                    }

                    var savedOrderId = _orderService.CreateAwaitingPayment(customerId.Value, order, orderId, checkoutRequestId);

                    _cartService.ClearCart();
                    HttpContext.Session.Remove($"CheckoutDraft_{customerId.Value}");
                    await HttpContext.Session.CommitAsync(HttpContext.RequestAborted);
                    return await StartStripeCheckout(savedOrderId, order, HttpContext.RequestAborted);
                }
                catch (Stripe.StripeException ex)
                {
                    _logger.LogError(ex, "Unable to check or close the previous Stripe session.");
                    TempData["ErrorMessage"] = Resource.PreviousPaymentCheckFailed;
                    return RedirectToAction("Index", "Cart", new { orderId });
                }
                catch (ArgumentException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                    return RedirectToAction("Index", "Cart", new { orderId });
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Unable to update the unpaid order.");
                    TempData["ErrorMessage"] = Resource.OrderUpdateFailed;
                    return RedirectToAction("Index", "Cart", new { orderId });
                }
            });
        }

        // Customer: Order history
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult MyOrders(int page = 1)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var orders = _orderService.GetPagedOrdersByCustomer(customerId.Value, page, 5);

            return View(orders);
        }

        // Display customer order edit form
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var order = _orderService.GetById(id);

            if (order == null)
                return NotFound();

            if (order.CustId != customerId.Value)
                return Forbid();

            if (order.OrderStatus != OrderStatus.OrderPlaced)
            {
                TempData["ErrorMessage"] = Resource.OrderEditNotAllowed;

                return RedirectToAction(nameof(Details), new { id });
            }

            var details = _orderService.GetOrderDetails(id);
            if (details == null)
                return NotFound();

            var model = new OrderViewModel
            {
                OrderId = order.OrderId,
                RequiredDate = order.RequiredDate?.Date,
                RequiredTime = order.RequiredDate?.TimeOfDay,
                OrderItems = details.OrderItems
            };

            return View(model);
        }

        // Save customer order changes
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrderViewModel model)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var order = _orderService.GetById(model.OrderId);

            if (order == null)
                return NotFound();

            if (order.CustId != customerId.Value)
                return Forbid();

            if (order.OrderStatus != OrderStatus.OrderPlaced)
            {
                TempData["ErrorMessage"] = Resource.OrderEditNotAllowed;

                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            if (!ModelState.IsValid)
            { 
                RestoreOrderItemNames(model);
                return View(model);
            }

            try
            {
                _orderService.UpdateCustomerOrder(model.OrderId, customerId.Value, model);

                TempData["SuccessMessage"] = Resource.OrderUpdatedSuccess;

                return RedirectToAction(nameof(Details), new { id = model.OrderId });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resource.OrderUpdateFailed);

                ModelState.AddModelError("", Resource.OrderUpdateFailed);
            }

            RestoreOrderItemNames(model);
            return View(model);
        }

        [Authorize(Roles = "Customer,Baker")]
        [HttpGet]
        public IActionResult Details(int id)
        {
            int? userId = GetCurrentUserId();

            if (!userId.HasValue)
                return Challenge();

            // Load order first for authorization
            var order = _orderService.GetById(id);

            if (order == null)
                return NotFound();

            if (User.IsInRole("Baker") && (order.OrderStatus == OrderStatus.PendingPayment || order.OrderStatus == OrderStatus.Cancelled))
            {
                return NotFound();
            }

            // Customer can only view their own orders
            if (User.IsInRole("Customer") &&
                order.CustId != userId.Value)
            {
                return Forbid();
            }

            // Retrieve joined order details
            var model = _orderService.GetOrderDetails(id);

            if (model == null)
                return NotFound();

            return View(model);
        }

        [Authorize(Roles = "Customer,Baker")]
        [HttpGet]
        public IActionResult CakeImage(int id, int cakeId, [FromServices] ICakeService cakeService)
        {
            var userId = GetCurrentUserId();

            if (!userId.HasValue)
                return Challenge();

            var details = _orderService.GetOrderDetails(id);

            if (details == null)
                return NotFound();

            if (User.IsInRole("Customer") && details.Order.CustId != userId.Value)
                return Forbid();

            if (User.IsInRole("Baker") && details.Order.OrderStatus == OrderStatus.PendingPayment || details.Order.OrderStatus == OrderStatus.Cancelled)
            {
                return NotFound();
            }

            if (!details.OrderItems.Any(item => item.CakeId == cakeId))
                return NotFound();

            var image = cakeService.GetById(cakeId)?.Image;

            if (image == null || image.ImageContent is not { Length: > 0 })
                return NotFound();

            return File(image.ImageContent, image.ImageType);
        }

        // Baker: All orders
        [Authorize(Roles = "Baker")]
        [HttpGet]
        public IActionResult Index(int page = 1, string sort = "")
        {
            var validChoices = new[]
            {
                "", "nearest", "pending", "preparing", "ready", "completed", "rejected"
            };

            if (Array.IndexOf(validChoices, sort) < 0)
                return NotFound();

            var orders = _orderService.GetPagedOrders(page, 5, sort);

            return View(orders);
        }

        // Baker: Update order status
        [Authorize(Roles = "Baker")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        private Task<IActionResult> RunCheckoutLockedAsync(int customerId, Func<Task<IActionResult>> checkout)
        {
            return CheckoutLock.RunAsync(
                _configuration,
                customerId,
                checkout,
                () => StatusCode(503, Resource.PreviousPaymentProcessing),
                HttpContext.RequestAborted);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayNow(int id)
        {
            var customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            return await RunCheckoutLockedAsync(customerId.Value, async () =>
            {
                var details = _orderService.GetOrderDetails(id);

                if (details == null)
                    return NotFound();

                if (details.Order.CustId != customerId.Value)
                    return Forbid();

                if (details.Order.OrderStatus != OrderStatus.PendingPayment || details.Payment?.PaymentStatus != PaymentStatus.Pending)
                {
                    return RedirectToAction(nameof(Details), new { id });
                }

                var sessionId = details.Payment.StripeCheckoutSessionId;
                var cancellationToken = HttpContext.RequestAborted;

                try
                {
                    if (!string.IsNullOrWhiteSpace(sessionId))
                    {
                        var stripeSession = await _stripeClient.V1.Checkout.Sessions.GetAsync(sessionId, cancellationToken: cancellationToken);

                        if (stripeSession.PaymentStatus == "paid")
                            return RedirectToAction(nameof(Payment), new { sessionId });

                        if (stripeSession.Status == "open" && !string.IsNullOrWhiteSpace(stripeSession.Url) && details.Order.RequiredDate > DateTime.Now)
                        {
                            return Redirect(stripeSession.Url);
                        }

                        if (stripeSession.Status != "expired" || stripeSession.PaymentStatus != "unpaid")
                        {
                            TempData["ErrorMessage"] = details.Order.RequiredDate <= DateTime.Now
                                ? Resource.PickupTimePassed
                                : Resource.PreviousPaymentProcessing;

                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }

                    if (!details.Order.RequiredDate.HasValue || details.Order.RequiredDate.Value <= DateTime.Now)
                    {
                        TempData["ErrorMessage"] = Resource.PickupTimePassed;
                        return RedirectToAction(nameof(Details), new { id });
                    }

                    if (!string.IsNullOrWhiteSpace(sessionId))
                        _orderService.ClearStripeSession(id, customerId.Value, sessionId);

                    var order = new OrderViewModel
                    {
                        Cart = new CartViewModel
                        {
                            Items = details.OrderItems.Select(item => new CartItemViewModel
                            {
                                CakeName = item.Cake?.CakeName ?? "Cake",
                                SizeName = item.Size?.SizeName ?? "",
                                FlavourName = item.Flavour?.FlavourName ?? "",
                                TierName = item.Tier?.TierName ?? "",
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice
                            }).ToList()
                        }
                    };

                    return await StartStripeCheckout(id, order, cancellationToken);
                }
                catch (Stripe.StripeException ex)
                {
                    _logger.LogError(ex, "Unable to reopen Stripe Checkout for order {OrderId}.", id);
                    TempData["ErrorMessage"] = Resource.StripeCheckoutStartFailed;
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Unable to update checkout for order {OrderId}.", id);
                    TempData["ErrorMessage"] = Resource.StripePaymentLinkChanged;
                }

                return RedirectToAction(nameof(Details), new { id });
            });
        }


        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelUnpaidOrder(int id)
        {
            var customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            return await RunCheckoutLockedAsync(customerId.Value, async () =>
            {
                var details = _orderService.GetOrderDetails(id);

                if (details == null)
                    return NotFound();

                if (details.Order.CustId != customerId.Value)
                    return Forbid();

                if (details.Order.OrderStatus == OrderStatus.Cancelled)
                    return RedirectToAction(nameof(Details), new { id });

                var payment = details.Payment;

                if (details.Order.OrderStatus != OrderStatus.PendingPayment || payment == null || payment.PaymentStatus != PaymentStatus.Pending)
                {
                    TempData["ErrorMessage"] = Resource.OrderCannotBeCancelled;
                    return RedirectToAction(nameof(Details), new { id });
                }

                var sessionId = payment.StripeCheckoutSessionId;
                var cancellationToken = HttpContext.RequestAborted;

                try
                {
                    if (!string.IsNullOrWhiteSpace(sessionId))
                    {
                        var stripeSessions = _stripeClient.V1.Checkout.Sessions;
                        var stripeSession = await stripeSessions.GetAsync(sessionId, cancellationToken: cancellationToken);

                        if (stripeSession.Status == "open" && stripeSession.PaymentStatus == "unpaid")
                        {
                            try
                            {
                                stripeSession = await stripeSessions.ExpireAsync(sessionId, cancellationToken: cancellationToken);
                            }
                            catch (Stripe.StripeException)
                            {
                                // Payment may have finished while we tried to expire the link.
                                stripeSession = await stripeSessions.GetAsync(sessionId, cancellationToken: cancellationToken);
                            }
                        }

                        if (stripeSession.PaymentStatus == "paid")
                            return RedirectToAction(nameof(Payment), new { sessionId });

                        if (stripeSession.Status != "expired" || stripeSession.PaymentStatus != "unpaid")
                        {
                            TempData["ErrorMessage"] = Resource.PreviousPaymentProcessing;
                            return RedirectToAction(nameof(Details), new { id });
                        }
                    }

                    _orderService.CancelUnpaidOrder(id, customerId.Value, sessionId, Resource.OrderCancelledByCustomer);

                    TempData["SuccessMessage"] = Resource.OrderCancelledSuccess;
                }
                catch (Stripe.StripeException ex)
                {
                    _logger.LogError(ex, "Unable to verify cancellation for order {OrderId}.", id);
                    TempData["ErrorMessage"] = Resource.PreviousPaymentCheckFailed;
                }
                catch (UnauthorizedAccessException)
                {
                    return Forbid();
                }
                catch (InvalidOperationException ex)
                {
                    TempData["ErrorMessage"] = ex.Message;
                }

                return RedirectToAction(nameof(Details), new { id });
            });
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> Payment(string? sessionId)
        {
            var customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            if (string.IsNullOrWhiteSpace(sessionId))
                return RedirectToAction(nameof(MyOrders));

            try
            {
                var stripeSession = await _stripeClient.V1.Checkout.Sessions.GetAsync(
                    sessionId,
                    cancellationToken: HttpContext.RequestAborted);

                if (stripeSession.Livemode || stripeSession.PaymentStatus != "paid" || stripeSession.Currency != "myr" || !stripeSession.AmountTotal.HasValue)
                {
                    return BadRequest(Resource.StripePaymentNotVerified);
                }

                var orderId = _orderService.ConfirmStripePayment(customerId.Value,stripeSession.Id,stripeSession.AmountTotal.Value);

                TempData["SuccessMessage"] = Resource.OrderPlacedSuccess;

                return RedirectToAction(nameof(Details), new { id = orderId });
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex, "Unable to verify Stripe Checkout payment.");
                return StatusCode(502, Resource.StripeVerificationUnavailable);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Unable to confirm the saved order payment.");
                return StatusCode(500, Resource.StripePaidOrderSaveFailed);
            }
        }

        private async Task<IActionResult> StartStripeCheckout(int orderId, OrderViewModel order, CancellationToken cancellationToken)
        {
            var customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            try
            {
                long ToSen(decimal amount) => (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

                var lineItems = order.Cart.Items.Select(item => new Stripe.Checkout.SessionLineItemOptions
                {
                    Quantity = item.Quantity,
                    PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                    {
                        Currency = "myr",
                        UnitAmount = ToSen(item.UnitPrice),
                        ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.CakeName,
                            Description = $"{item.SizeName} · {item.FlavourName} · Tier {item.TierName}"
                        }
                    }
                }).ToList();

                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    Mode = "payment",
                    AllowedPaymentMethodTypes = new List<string> { "card" },
                    CancelUrl = Url.Action(nameof(Details), "Order", new { id = orderId }, Request.Scheme),
                    SuccessUrl = "https://localhost:7056/Order/Payment?sessionId={CHECKOUT_SESSION_ID}",
                    LineItems = lineItems,
                };

                var session = await _stripeClient.V1.Checkout.Sessions.CreateAsync(options, cancellationToken: cancellationToken);
                _orderService.SaveStripeSession(orderId, customerId.Value, session.Id);
                return Redirect(session.Url);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Cart", new { orderId });
            }
            catch (Stripe.StripeException)
            {
                TempData["ErrorMessage"] = Resource.StripeCheckoutStartFailed;
                return RedirectToAction(nameof(Details), new { id = orderId });
            }
        }
    }


}
