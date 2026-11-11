
using CakeStudio.Constant;
using CakeStudio.Models;
using CakeStudio.Resources;
using CakeStudio.Services;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace CakeStudio.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderController> _logger;
        private readonly Stripe.StripeClient _stripeClient;
        public OrderController( IOrderService orderService,ICartService cartService, ILogger<OrderController> logger, Stripe.StripeClient stripeClient)
        {
            _orderService = orderService;
            _cartService = cartService;
            _logger = logger;
            _stripeClient = stripeClient;
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
        public async Task<IActionResult> Checkout(List<CartItemViewModel> items, DateTime? requiredDate, TimeSpan? requiredTime)
        {
            var customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Check the pickup date, time, and cake options.";
                return RedirectToAction("Index", "Cart");
            }

            var order = new OrderViewModel
            {
                RequiredDate = requiredDate,
                RequiredTime = requiredTime,
                CollectionMethod = CollectionMethod.Pickup
            };

            HttpContext.Session.SetString(
                $"CheckoutDraft_{customerId.Value}",
                JsonSerializer.Serialize(new { order.RequiredDate, order.RequiredTime }));

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

            if (!requiredDate.HasValue || !requiredTime.HasValue || requiredTime.Value < TimeSpan.Zero || requiredTime.Value >= TimeSpan.FromDays(1) || requiredDate.Value.Date.Add(requiredTime.Value) <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Choose a pickup date and time in the future.";
                return RedirectToAction("Index", "Cart");
            }

            var bookedQuantity = _orderService.GetBookedCakeQuantity(requiredDate.Value.Date);
            var remaining = Math.Max(0, BakingCapacity.DailyLimit - bookedQuantity);

            if (order.Cart.TotalQuantity > remaining)
            {
                TempData["ErrorMessage"] = remaining == 0
                    ? Resource.DateFullyBooked
                    : $"Only {remaining} more {(remaining == 1 ? "cake" : "cakes")} can be booked for this date.";

                return RedirectToAction("Index", "Cart");
            }

            return await StartStripeCheckout(order, HttpContext.RequestAborted);
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

            if (order.OrderStatus != OrderStatus.Pending)
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
                CollectionMethod = order.CollectionMethod,
                DeliveryAddress = order.DeliveryAddress,
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

            if (order.OrderStatus != OrderStatus.Pending)
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
        public IActionResult UpdateStatus(int orderId, string status, string? rejectionReason)
        {
            int? bakerId = GetCurrentUserId();

            if (!bakerId.HasValue)
                return Challenge();

            try
            {
                _orderService.UpdateStatus(orderId, status, bakerId.Value, rejectionReason);

                TempData["SuccessMessage"] = Resource.OrderStatusUpdatedSuccess;
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resource.OrderStatusUpdateFailed);
                TempData["ErrorMessage"] = Resource.OrderStatusUpdateFailed;
            }

            return RedirectToAction(nameof(Index));
        }

        //	Payment Summary
        private PaymentViewModel BuildPaymentViewModel(OrderViewModel order)
        {
            if (!order.Cart.Items.Any())
                throw new ArgumentException(Resource.CartEmpty);

            foreach (var item in order.Cart.Items)
            {
                if (item.Quantity < 1 || item.Quantity > 20)
                {
                    throw new ArgumentException(Resource.InvalidCakeQuantity);
                }

                // Recheck availability and calculate the current price
                item.UnitPrice = _orderService.CalculateTotalPrice(
                    new OrderViewModel
                    {
                        CakeId = item.CakeId,
                        SizeId = item.SizeId,
                        FlavourId = item.FlavourId,
                        TierId = item.TierId
                    });
            }

            decimal deliveryFee = order.CollectionMethod == CollectionMethod.Delivery
                ? DeliveryRate.FixedFee
                : 0m;

            decimal totalPrice = order.Cart.TotalPrice + deliveryFee;

            if (totalPrice <= 0 || totalPrice > 100000)
                throw new ArgumentException(Resource.InvalidOrderPrice);

            return new PaymentViewModel
            {
                Cart = order.Cart,
                Amount = totalPrice,
                DeliveryFee = deliveryFee,
                PaymentMethod = PaymentMethod.Card,
                RequiredDate = order.RequiredDate,
                RequiredTime = order.RequiredTime,
                CollectionMethod = order.CollectionMethod
            };
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> Payment([FromQuery(Name = "session_id")] string? sessionId)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            string pendingOrderKey = $"PendingOrder_{customerId.Value}";

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                pendingOrderKey = $"PendingOrder_{customerId.Value}_{sessionId}";
            }

            var pendingOrder = HttpContext.Session.GetString(pendingOrderKey);

            if (string.IsNullOrEmpty(pendingOrder))
                return RedirectToAction(nameof(MyOrders));

            var order = JsonSerializer.Deserialize<OrderViewModel>(pendingOrder);

            if (order == null)
                return RedirectToAction(nameof(MyOrders));

            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                var savedAmount = HttpContext.Session.GetString($"StripeAmount_{sessionId}");

                if (!long.TryParse(savedAmount, out var expectedAmount))
                {
                    return BadRequest(Resource.StripeSessionMismatch);
                }

                try
                {
                    var stripeSession = await _stripeClient.V1.Checkout.Sessions.GetAsync(sessionId, cancellationToken: HttpContext.RequestAborted);

                    if (stripeSession.Livemode || stripeSession.PaymentStatus != "paid" || stripeSession.Currency != "myr" || stripeSession.AmountTotal != expectedAmount)
                    {
                        return BadRequest(Resource.StripePaymentNotVerified);
                    }

                    int orderId;

                    try
                    {
                        orderId = _orderService.Create(
                            customerId.Value,
                            order,
                            PaymentMethod.Card,
                            stripeCheckoutSessionId: stripeSession.Id,
                            stripeAmountInSen: expectedAmount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to save the order after Stripe payment verification.");
                        return StatusCode(500, Resource.StripePaidOrderSaveFailed);
                    }

                    _cartService.ClearCart();
                    HttpContext.Session.Remove(pendingOrderKey);
                    HttpContext.Session.Remove($"PendingOrder_{customerId.Value}");
                    HttpContext.Session.Remove($"CheckoutDraft_{customerId.Value}");
                    HttpContext.Session.Remove($"StripeAmount_{stripeSession.Id}");

                    TempData["SuccessMessage"] = Resource.OrderPlacedSuccess;
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }
                catch (Stripe.StripeException ex)
                {
                    _logger.LogError(ex, "Unable to verify Stripe Checkout payment.");
                    return StatusCode(502, Resource.StripeVerificationUnavailable);
                }
            }

            return RedirectToAction(nameof(Checkout));
        }

        private async Task<IActionResult> StartStripeCheckout(OrderViewModel order, CancellationToken cancellationToken)
        {
            var customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            try
            {
                var summary = BuildPaymentViewModel(order);
                long amountInSen = (long)decimal.Round(summary.Amount * 100m, 0, MidpointRounding.AwayFromZero);

                const string paymentUrl = "https://localhost:7056/Order/Payment";
                const string checkoutUrl = "https://localhost:7056/Cart";

                long ToSen(decimal amount) => (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

                var lineItems = summary.Cart.Items.Select(item => new Stripe.Checkout.SessionLineItemOptions
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

                if (summary.DeliveryFee > 0)
                {
                    lineItems.Add(new Stripe.Checkout.SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            Currency = "myr",
                            UnitAmount = ToSen(summary.DeliveryFee),
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Delivery fee"
                            }
                        }
                    });
                }

                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    Mode = "payment",
                    AllowedPaymentMethodTypes = new List<string> { "card" },
                    CancelUrl = checkoutUrl,
                    SuccessUrl = paymentUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    LineItems = lineItems,
                };

                var session = await _stripeClient.V1.Checkout.Sessions.CreateAsync(options, cancellationToken: cancellationToken);
                HttpContext.Session.SetString($"PendingOrder_{customerId.Value}_{session.Id}",JsonSerializer.Serialize(order));
                HttpContext.Session.SetString($"StripeAmount_{session.Id}", amountInSen.ToString());
                return Redirect(session.Url);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Cart");
            }
            catch (Stripe.StripeException)
            {
                TempData["ErrorMessage"] = Resource.StripeCheckoutStartFailed;
                return RedirectToAction(nameof(Checkout));
            }
        }
    }


}
