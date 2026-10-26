
using CakeStudio.Models;
using CakeStudio.Services;
using CakeStudio.Services.Interfaces;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CakeStudio.Resources;
using System.Text.Json;
using CakeStudio.Constant;

namespace CakeStudio.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ICakeService _cakeService;
        private readonly ICakeSizeService _sizeService;
        private readonly ICakeFlavourService _flavourService;
        private readonly ICakeTierService _tierService;

        public OrderController( IOrderService orderService, ICakeService cakeService, ICakeSizeService sizeService,ICakeFlavourService flavourService,ICakeTierService tierService)
        {
            _orderService = orderService;
            _cakeService = cakeService;
            _sizeService = sizeService;
            _flavourService = flavourService;
            _tierService = tierService;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id
                : null;
        }

        // untuk retrieve customer selected option in form again when invalid
        private void LoadCakeInfo(OrderViewModel model) 
        {
            if (model.CakeId.HasValue)
            {
                var cake = _cakeService.GetById(model.CakeId.Value);

                if (cake != null)
                {
                    model.CakeName = cake.CakeName;
                    model.BasePrice = cake.BasePrice;

                    decimal sizePrice = 0m;
                    decimal flavourPrice = 0m;
                    decimal tierPrice = 0m;

                    // Retrieve selected size
                    if (model.SizeId.HasValue)
                    {
                        var size = _sizeService.GetById(model.SizeId.Value);

                        if (size != null && size.IsAvailable)
                        {
                            ViewBag.SizeName = size.SizeName;

                            bool isDefaultSize = size.SizeName.Trim()
                                .Equals("6 inch", StringComparison.OrdinalIgnoreCase);

                            if (!isDefaultSize)
                            {
                                sizePrice = size.AdditionalPrice;
                            }
                        }
                    }

                    // Retrieve selected flavour
                    if (model.FlavourId.HasValue)
                    {
                        var flavour = _flavourService.GetById(model.FlavourId.Value);

                        if (flavour != null && flavour.IsAvailable)
                        {
                            ViewBag.FlavourName = flavour.FlavourName;

                            if (cake.Flavour != null)
                            {
                                bool isDefaultFlavour =
                                    flavour.FlavourId == cake.Flavour.FlavourId;

                                if (!isDefaultFlavour)
                                {
                                    flavourPrice = flavour.AdditionalPrice;
                                }
                            }
                        }
                    }

                    // Retrieve selected tier
                    if (model.TierId.HasValue)
                    {
                        var tier = _tierService.GetById(model.TierId.Value);

                        if (tier != null && tier.IsAvailable)
                        {
                            ViewBag.TierName = tier.TierName;

                            bool isDefaultTier = tier.TierName.Trim()
                                .Equals("1", StringComparison.OrdinalIgnoreCase);

                            if (!isDefaultTier)
                            {
                                tierPrice = tier.AdditionalPrice;
                            }
                        }
                    }

                    // Restore additional prices
                    ViewBag.SizePrice = sizePrice;
                    ViewBag.FlavourPrice = flavourPrice;
                    ViewBag.TierPrice = tierPrice;

                    // Restore total price
                    ViewBag.TotalPrice =
                        cake.BasePrice + sizePrice + flavourPrice + tierPrice;
                }
            }

            LoadCakeOptions();
        }

        private void LoadCakeOptions()
        {
            ViewBag.Sizes = _sizeService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            ViewBag.Flavours = _flavourService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            ViewBag.Tiers = _tierService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();
        }


        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Create(int cakeId,int? sizeId,int? flavourId,int? tierId)
        {
            var cake = _cakeService.GetById(cakeId);

            if (cake == null || !cake.IsAvailable)
                return NotFound();

            var sizes = _sizeService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            var flavours = _flavourService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            var tiers = _tierService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            //match tak data atas ngan cust selected id 
            var size = sizes.FirstOrDefault(x => x.SizeId == sizeId);
            var flavour = flavours.FirstOrDefault(x => x.FlavourId == flavourId);
            var tier = tiers.FirstOrDefault(x => x.TierId == tierId);

            if (size == null || flavour == null || tier == null)
                return BadRequest(Resource.InvalidCakeOptions);

            //create object
            var model = new OrderViewModel
            {
                CakeId = cake.CakeId,
                CakeName = cake.CakeName,
                BasePrice = cake.BasePrice,
                SizeId = size.SizeId,
                FlavourId = flavour.FlavourId,
                TierId = tier.TierId
            };

            ViewBag.SizeName = size.SizeName;
            ViewBag.FlavourName = flavour.FlavourName;
            ViewBag.TierName = tier.TierName;


            decimal totalPrice = cake.BasePrice;

            //  inch is included
            bool isDefaultSize = size.SizeName.Trim()
                .Equals("6 inch", StringComparison.OrdinalIgnoreCase);

            if (!isDefaultSize)
            {
                totalPrice += size.AdditionalPrice;
            }

            // ori flav
            bool isDefaultFlavour =
                flavour.FlavourId == cake.Flavour.FlavourId;

            if (!isDefaultFlavour)
            {
                totalPrice += flavour.AdditionalPrice;
            }

            // 1 tier included
            bool isDefaultTier = tier.TierName.Trim()
                .Equals("1", StringComparison.OrdinalIgnoreCase);

            if (!isDefaultTier)
            {
                totalPrice += tier.AdditionalPrice;
            }

            // nak pass > UI (OrderViewModel read ID)
            // 6 inch included as default
            if (isDefaultSize)
            {
                ViewBag.SizePrice = 0m;
            }
            else
            {
                ViewBag.SizePrice = size.AdditionalPrice;
            }


          
            if (isDefaultFlavour)
            {
                ViewBag.FlavourPrice = 0m;
            }
            else
            {
                ViewBag.FlavourPrice = flavour.AdditionalPrice;
            }



            if (isDefaultTier)
            {
                ViewBag.TierPrice = 0m;
            }
            else
            {
                ViewBag.TierPrice = tier.AdditionalPrice;
            }

            ViewBag.TotalPrice = totalPrice;


            return View(model);
        }

        // Customer: Submit order
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrderViewModel model)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge(); //gi login page //ask the authentication system to authenticate the user //uses existing authentication configuration //configured LoginPath, ASP.NET Core knows where to redirect when an authentication challenge occurs.[program.cs]

            if (model.RequiredDate.HasValue && model.RequiredDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(model.RequiredDate), Resource.RequiredDateInPast);
            }

            if (model.CollectionMethod == "Delivery" && string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                ModelState.AddModelError( nameof(model.DeliveryAddress),Resource.DeliveryAddressRequired);
            }

            if (!ModelState.IsValid)
            {
                LoadCakeInfo(model);
                return View(model);
            }

            try
            {
                HttpContext.Session.SetString("PendingOrder", JsonSerializer.Serialize(model));

                return RedirectToAction(nameof(Payment));
            }
            catch (ArgumentException ex) 
            {
                //hndle invlid orderr info // throw dalam orderservice create 
                ModelState.AddModelError(string.Empty, ex.Message);

                //Restores the cake information and displays the order form again
                LoadCakeInfo(model);
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty,Resource.OrderPlacementFailed);

                LoadCakeInfo(model);
                return View(model);
            }
        }

        // Customer: Order history
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult MyOrders()
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var orders = _orderService
                .GetOrdersByCustomer(customerId.Value);

            return View(orders);
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
        public IActionResult Index()
        {
            var orders = _orderService.GetAll();

            return View(orders);
        }

        // Baker: Update order status
        [Authorize(Roles = "Baker")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int orderId, string status)
        {
            int? bakerId = GetCurrentUserId();

            if (!bakerId.HasValue)
                return Challenge();

            try
            {
                _orderService.UpdateStatus(
                    orderId, status, bakerId.Value);

                TempData["SuccessMessage"] =
                    Resource.OrderStatusUpdatedSuccess;
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
            catch (Exception)
            {
                TempData["ErrorMessage"] =
                    Resource.OrderStatusUpdateFailed;
            }

            return RedirectToAction(nameof(Index));
        }

        private PaymentViewModel BuildPaymentViewModel(OrderViewModel order)
        {
            var cake = _cakeService.GetById(order.CakeId!.Value);
            var size = _sizeService.GetById(order.SizeId!.Value);
            var flavour = _flavourService.GetById(order.FlavourId!.Value);
            var tier = _tierService.GetById(order.TierId!.Value);

            if (cake == null || size == null || flavour == null || tier == null)
                throw new ArgumentException("Invalid cake or cake options.");

            bool isDefaultSize = size.SizeName.Trim()
                .Equals("6 inch", StringComparison.OrdinalIgnoreCase);

            bool isDefaultFlavour = cake.Flavour != null &&
                flavour.FlavourId == cake.Flavour.FlavourId;

            bool isDefaultTier = tier.TierName.Trim()
                .Equals("1", StringComparison.OrdinalIgnoreCase);

            return new PaymentViewModel
            {
                Amount = _orderService.CalculateTotalPrice(order),
                PaymentMethod = PaymentMethod.Card,

                CakeName = cake.CakeName,
                BasePrice = cake.BasePrice,

                SizeName = size.SizeName,
                SizePrice = isDefaultSize ? 0m : size.AdditionalPrice,

                FlavourName = flavour.FlavourName,
                FlavourPrice = isDefaultFlavour ? 0m : flavour.AdditionalPrice,

                TierName = tier.TierName,
                TierPrice = isDefaultTier ? 0m : tier.AdditionalPrice,

                RequiredDate = order.RequiredDate,
                RequiredTime = order.RequiredTime,
                CollectionMethod = order.CollectionMethod
            };
        }

        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Payment()
        {
            var pendingOrder = HttpContext.Session.GetString("PendingOrder");

            if (string.IsNullOrEmpty(pendingOrder))
                return RedirectToAction(nameof(MyOrders));

            var order = JsonSerializer.Deserialize<OrderViewModel>(pendingOrder);

            if (order == null)
                return RedirectToAction(nameof(MyOrders));

            var model = BuildPaymentViewModel(order);
            return View(model);
        }


        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Payment(PaymentViewModel model)
        {
            int? customerId = GetCurrentUserId();

            if (!customerId.HasValue)
                return Challenge();

            var pendingOrder = HttpContext.Session.GetString("PendingOrder");

            if (string.IsNullOrEmpty(pendingOrder))
                return RedirectToAction(nameof(MyOrders));

            var order = JsonSerializer.Deserialize<OrderViewModel>(pendingOrder);

            if (order == null)
                return RedirectToAction(nameof(MyOrders));

            // Validate predefined dummy card details only
            string cardNumber = (model.CardNumber ?? "")
                .Replace(" ", "");

            string expiryDate = (model.ExpiryDate ?? "").Trim();
            string securityCode = (model.SecurityCode ?? "").Trim();

            // Validate card number format (16 digits)
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                cardNumber, @"^\d{16}$"))
            {
                ModelState.AddModelError(
                    nameof(model.CardNumber),
                    "Card number must contain 16 digits.");
            }

            // Validate expiry date format and expiration
            if (!DateTime.TryParseExact(
                expiryDate,
                "MM/yy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime expiry))
            {
                ModelState.AddModelError(
                    nameof(model.ExpiryDate),
                    "Enter a valid expiry date (MM/YY).");
            }
            else
            {
                // Card is valid until the end of its expiry month
                DateTime currentMonth = new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1);

                if (expiry.Year < currentMonth.Year ||
                    (expiry.Year == currentMonth.Year &&
                     expiry.Month < currentMonth.Month))
                {
                    ModelState.AddModelError(
                        nameof(model.ExpiryDate),
                        "Card has expired. Please enter a valid expiry date.");
                }
            }

            // Validate security code format (3 digits)
            if (!System.Text.RegularExpressions.Regex.IsMatch(
                securityCode, @"^\d{3}$"))
            {
                ModelState.AddModelError(
                    nameof(model.SecurityCode),
                    "Security code must contain 3 digits.");
            }

            if (!ModelState.IsValid)
            {
                var invalidSummary = BuildPaymentViewModel(order);

                // Preserve validation errors and the submitted test fields.
                model.Amount = invalidSummary.Amount;
                model.PaymentMethod = invalidSummary.PaymentMethod;
                model.CakeName = invalidSummary.CakeName;
                model.BasePrice = invalidSummary.BasePrice;
                model.SizeName = invalidSummary.SizeName;
                model.SizePrice = invalidSummary.SizePrice;
                model.FlavourName = invalidSummary.FlavourName;
                model.FlavourPrice = invalidSummary.FlavourPrice;
                model.TierName = invalidSummary.TierName;
                model.TierPrice = invalidSummary.TierPrice;
                model.RequiredDate = invalidSummary.RequiredDate;
                model.RequiredTime = invalidSummary.RequiredTime;
                model.CollectionMethod = invalidSummary.CollectionMethod;
                return View(model);
            }

            try
            {
                int orderId = _orderService.Create(
                    customerId.Value,
                    order,
                    PaymentMethod.Card
                );

                HttpContext.Session.Remove("PendingOrder");

                TempData["SuccessMessage"] = Resource.OrderPlacedSuccess;

                return RedirectToAction(nameof(Details), new { id = orderId });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                ModelState.AddModelError(
                    string.Empty,
                    "Payment could not be completed. Please try again."
                );
            }

            var summary = BuildPaymentViewModel(order);

            // Preserve validation errors and the submitted test fields.
            model.Amount = summary.Amount;
            model.PaymentMethod = summary.PaymentMethod;
            model.CakeName = summary.CakeName;
            model.BasePrice = summary.BasePrice;
            model.SizeName = summary.SizeName;
            model.SizePrice = summary.SizePrice;
            model.FlavourName = summary.FlavourName;
            model.FlavourPrice = summary.FlavourPrice;
            model.TierName = summary.TierName;
            model.TierPrice = summary.TierPrice;
            model.RequiredDate = summary.RequiredDate;
            model.RequiredTime = summary.RequiredTime;
            model.CollectionMethod = summary.CollectionMethod;

            return View(model);
        }
    }


}
