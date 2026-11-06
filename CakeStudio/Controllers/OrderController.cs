
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

        public OrderController( IOrderService orderService,ICartService cartService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _cartService = cartService;
            _logger = logger;
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

            var pendingOrderJson = HttpContext.Session.GetString($"PendingOrder_{customerId.Value}");

            if (!string.IsNullOrEmpty(pendingOrderJson))
            {
                var pendingOrder =
                    JsonSerializer.Deserialize<OrderViewModel>(pendingOrderJson);

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
        public IActionResult Checkout(OrderViewModel model)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

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
                ModelState.AddModelError(nameof(model.DeliveryAddress),
                    Resource.DeliveryAddressRequired);
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

            // Copy only the intended checkout information
            var pendingOrder = new OrderViewModel
            {
                CheckoutId = Guid.NewGuid().ToString("N"),
                RequiredDate = model.RequiredDate,
                RequiredTime = model.RequiredTime,
                CollectionMethod = model.CollectionMethod,
                DeliveryAddress = model.CollectionMethod == CollectionMethod.Delivery
                    ? model.DeliveryAddress?.Trim()
                    : null,
                Cart = model.Cart
            };

            HttpContext.Session.SetString(
                $"PendingOrder_{customerId.Value}",
                JsonSerializer.Serialize(pendingOrder));

            return RedirectToAction(nameof(Payment));
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
                CheckoutId = order.CheckoutId,
                Cart = order.Cart,
                Amount = totalPrice,
                DeliveryFee = deliveryFee,
                PaymentMethod = PaymentMethod.Card,
                RequiredDate = order.RequiredDate,
                RequiredTime = order.RequiredTime,
                CollectionMethod = order.CollectionMethod
            };
        }

        private IActionResult RedisplayPayment(OrderViewModel order,PaymentViewModel model,int customerId)
        {
            try
            {
                var summary = BuildPaymentViewModel(order);
                model.CheckoutId = summary.CheckoutId;
                model.Cart = summary.Cart;
                model.Amount = summary.Amount;
                model.DeliveryFee = summary.DeliveryFee;
                model.PaymentMethod = summary.PaymentMethod;
                model.RequiredDate = summary.RequiredDate;
                model.RequiredTime = summary.RequiredTime;
                model.CollectionMethod = summary.CollectionMethod;

                // Save refreshed prices for the customer's next payment attempt
                HttpContext.Session.SetString($"PendingOrder_{customerId}",JsonSerializer.Serialize(order));

                return View("Payment", model);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                return RedirectToAction("Index", "Cart");
            }
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Payment()
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var pendingOrder = HttpContext.Session.GetString($"PendingOrder_{customerId.Value}");

            if (string.IsNullOrEmpty(pendingOrder))
                return RedirectToAction(nameof(MyOrders));

            var order = JsonSerializer.Deserialize<OrderViewModel>(pendingOrder);

            if (order == null)
                return RedirectToAction(nameof(MyOrders));

            try
            {
                var model = BuildPaymentViewModel(order);
                HttpContext.Session.SetString($"PendingOrder_{customerId.Value}",JsonSerializer.Serialize(order));
                return View(model);
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index", "Cart");
            }
        }


        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Payment(PaymentViewModel model)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var pendingOrder = HttpContext.Session.GetString($"PendingOrder_{customerId.Value}");

            if (string.IsNullOrEmpty(pendingOrder))
                return RedirectToAction(nameof(MyOrders));

            var order = JsonSerializer.Deserialize<OrderViewModel>(pendingOrder);

            if (order == null)
                return RedirectToAction(nameof(MyOrders));

            if (string.IsNullOrEmpty(order.CheckoutId) || model.CheckoutId != order.CheckoutId)
            {
                TempData["ErrorMessage"] =
                    "Your checkout has changed. Please review it before paying.";

                return RedirectToAction(nameof(Checkout));
            }

            // Validate test card details entered by the customer
            string cardNumber = (model.CardNumber ?? "")
                .Replace(" ", "");

            string expiryDate = (model.ExpiryDate ?? "").Trim();
            string securityCode = (model.SecurityCode ?? "").Trim();

            // Validate card number format (16 digits)
            if (!System.Text.RegularExpressions.Regex.IsMatch(cardNumber, @"^\d{16}$"))
            {
                ModelState.AddModelError(
                    nameof(model.CardNumber), Resource.InvalidCardNumber);
            }

            // Validate expiry date format and expiration
            if (!DateTime.TryParseExact(
                expiryDate,
                "MM/yy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime expiry))
            {
                ModelState.AddModelError(nameof(model.ExpiryDate), Resource.InvalidCardExpiryDate);
            }
            else
            {
                // Card is valid until the end of its expiry month
                DateTime currentMonth = new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);

                if (expiry.Year < currentMonth.Year || (expiry.Year == currentMonth.Year && expiry.Month < currentMonth.Month))
                {
                    ModelState.AddModelError(nameof(model.ExpiryDate),Resource.CardExpired);
                }
            }

            // Validate security code format (3 digits)
            if (!System.Text.RegularExpressions.Regex.IsMatch(securityCode, @"^\d{3}$"))
            {
                ModelState.AddModelError(nameof(model.SecurityCode),Resource.InvalidSecurityCode);
            }

            if (!ModelState.IsValid)
            {
                return RedisplayPayment(order, model, customerId.Value);
            }

            try
            {
                int orderId = _orderService.Create(customerId.Value,order,PaymentMethod.Card);

                _cartService.ClearCart();

                HttpContext.Session.Remove($"PendingOrder_{customerId.Value}");

                TempData["SuccessMessage"] = Resource.OrderPlacedSuccess;

                return RedirectToAction(nameof(Details), new { id = orderId });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Resource.PaymentFailed);
                ModelState.AddModelError(string.Empty,Resource.PaymentFailed);
            }

          
            return RedisplayPayment(order, model, customerId.Value);
        }
    }


}
