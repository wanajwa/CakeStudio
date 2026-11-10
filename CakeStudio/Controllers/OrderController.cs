
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

        // Display checkout for all cart items
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Checkout()
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var cart = _cartService.GetCart();

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = Resource.CartEmpty;

                return RedirectToAction("Index", "Cart");
            }

            var model = new OrderViewModel();

            var pendingOrderJson =
                HttpContext.Session.GetString($"CheckoutDraft_{customerId.Value}")
                ?? HttpContext.Session.GetString($"PendingOrder_{customerId.Value}");

            if (!string.IsNullOrEmpty(pendingOrderJson))
            {
                var pendingOrder = JsonSerializer.Deserialize<OrderViewModel>(pendingOrderJson);

                if (pendingOrder != null)
                {
                    model.RequiredDate = pendingOrder.RequiredDate;
                    model.RequiredTime = pendingOrder.RequiredTime;
                    model.CollectionMethod = pendingOrder.CollectionMethod;
                    model.DeliveryAddress = pendingOrder.DeliveryAddress;
                }
            }

            model.Cart = cart;

            return View("Create", model);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(OrderViewModel model, string? intent)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var draft = new
            {
                model.RequiredDate,
                model.RequiredTime,
                model.CollectionMethod,
                model.DeliveryAddress
            };

            HttpContext.Session.SetString($"CheckoutDraft_{customerId.Value}",JsonSerializer.Serialize(draft));

            if (intent == "backToCart")
            {
                return RedirectToAction("Index", "Cart");
            }

            if (intent == "backToCakes")
            {
                return RedirectToAction("Index", "CustomerCake");
            }

            // Replace submitted cart data with the cart from Session
            model.Cart = _cartService.GetCart();

            if (!model.Cart.Items.Any())
            {
                TempData["ErrorMessage"] = Resource.CartEmpty;
                return RedirectToAction("Index", "Cart");
            }

            // Validate date and time
            if (model.RequiredDate.HasValue && model.RequiredTime.HasValue)
            {
                var time = model.RequiredTime.Value;

                if (time < TimeSpan.Zero || time >= TimeSpan.FromDays(1) || model.RequiredDate.Value.Date.Add(time) <= DateTime.Now)
                {
                    ModelState.AddModelError(nameof(model.RequiredDate),Resource.InvalidRequiredDateTime);
                }
            }

            // Validate collection method
            if (model.CollectionMethod != CollectionMethod.Pickup && model.CollectionMethod != CollectionMethod.Delivery)
            {
                ModelState.AddModelError(nameof(model.CollectionMethod),Resource.InvalidCollectionMethod);
            }

            // Delivery requires an address
            if (model.CollectionMethod == CollectionMethod.Delivery && string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                ModelState.AddModelError(nameof(model.DeliveryAddress), Resource.DeliveryAddressRequired);
            }

            try
            {
                foreach (var item in model.Cart.Items)
                {
                    if (item.Quantity < 1 || item.Quantity > 20)
                    {
                        throw new ArgumentException(Resource.InvalidCakeQuantity);
                    }

                    // Validate availability and calculate database price
                    item.UnitPrice = _orderService.CalculateTotalPrice(
                        new OrderViewModel
                        {
                            CakeId = item.CakeId,
                            SizeId = item.SizeId,
                            FlavourId = item.FlavourId,
                            TierId = item.TierId
                        });
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            if (model.RequiredDate.HasValue)
            {
                int bookedQuantity = _orderService.GetBookedCakeQuantity(model.RequiredDate.Value);

                int requestedQuantity = model.Cart.Items.Sum(item => item.Quantity);

                int remaining = Math.Max(0, BakingCapacity.DailyLimit - bookedQuantity);

                if (requestedQuantity > remaining)
                {
                    string message = remaining == 0
                        ? Resource.DateFullyBooked
                        : $"Only {remaining} more {(remaining == 1 ? "cake" : "cakes")} can be booked for this date.";

                    ModelState.AddModelError("", message);
                }
            }


            if (!ModelState.IsValid)
                return View("Create", model);

      
            var pendingOrder = new OrderViewModel
            {
                RequiredDate = model.RequiredDate,
                RequiredTime = model.RequiredTime,
                CollectionMethod = model.CollectionMethod,
                DeliveryAddress = model.CollectionMethod == CollectionMethod.Delivery
                    ? model.DeliveryAddress?.Trim()
                    : null,
                Cart = model.Cart
            };

            HttpContext.Session.SetString($"PendingOrder_{customerId.Value}",JsonSerializer.Serialize(pendingOrder));

            return await StartStripeCheckout(pendingOrder, HttpContext.RequestAborted);
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
                    return BadRequest("This Stripe session could not be matched to your checkout.");
                }

                try
                {
                    var stripeSession = await _stripeClient.V1.Checkout.Sessions.GetAsync(
                        sessionId, cancellationToken: HttpContext.RequestAborted);

                    if (stripeSession.Livemode || stripeSession.PaymentStatus != "paid" || stripeSession.Currency != "myr" || stripeSession.AmountTotal != expectedAmount)
                    {
                        return BadRequest("The Stripe payment could not be verified.");
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
                        return StatusCode(500, "Stripe verified the payment, but the order could not be saved. " + "Refresh this page to retry saving the order.");
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
                    return StatusCode(502, "Unable to verify the Stripe payment. Refresh to retry.");
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
                const string checkoutUrl = "https://localhost:7056/Order/Checkout";

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
                TempData["ErrorMessage"] = "Unable to open Stripe Checkout. Please try again.";
                return RedirectToAction(nameof(Checkout));
            }
        }
    }


}
