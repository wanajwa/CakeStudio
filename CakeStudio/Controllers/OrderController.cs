
using CakeStudio.Models;
using CakeStudio.Services;
using CakeStudio.Services.Interfaces;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        public OrderController(
            IOrderService orderService,
            ICakeService cakeService,
            ICakeSizeService sizeService,
            ICakeFlavourService flavourService,
            ICakeTierService tierService)
        {
            _orderService = orderService;
            _cakeService = cakeService;
            _sizeService = sizeService;
            _flavourService = flavourService;
            _tierService = tierService;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id
                : null;
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


        private void LoadCakeInfo(OrderViewModel model)
        {
            if (model.CakeId.HasValue)
            {
                var cake = _cakeService.GetById(
                    model.CakeId.Value);

                if (cake != null)
                {
                    model.CakeName = cake.CakeName;
                    model.BasePrice = cake.BasePrice;
                }
            }

            LoadCakeOptions();
        }


        [Authorize(Roles = "Customer")]
        [HttpGet]
        public IActionResult Create(
            int cakeId,
            int? sizeId,
            int? flavourId,
            int? tierId)
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

            var size = sizes.FirstOrDefault(x => x.SizeId == sizeId);
            var flavour = flavours.FirstOrDefault(x => x.FlavourId == flavourId);
            var tier = tiers.FirstOrDefault(x => x.TierId == tierId);

            if (size == null || flavour == null || tier == null)
                return BadRequest("Invalid cake options selected.");

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

            // Size: 6 inch is included
            bool isDefaultSize = size.SizeName.Trim()
                .Equals("6 inch", StringComparison.OrdinalIgnoreCase);

            if (!isDefaultSize)
            {
                totalPrice += size.AdditionalPrice;
            }

            // Flavour: cake's original flavour is included
            bool isDefaultFlavour =
                flavour.FlavourId == cake.Flavour.FlavourId;

            if (!isDefaultFlavour)
            {
                totalPrice += flavour.AdditionalPrice;
            }

            // Tier: 1 tier is included
            bool isDefaultTier = tier.TierName.Trim()
                .Equals("1", StringComparison.OrdinalIgnoreCase);

            if (!isDefaultTier)
            {
                totalPrice += tier.AdditionalPrice;
            }


            ViewBag.SizePrice = isDefaultSize ? 0m : size.AdditionalPrice;

            ViewBag.FlavourPrice = isDefaultFlavour ? 0m : flavour.AdditionalPrice;

            ViewBag.TierPrice = isDefaultTier ? 0m : tier.AdditionalPrice;


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
                return Challenge();

            if (model.RequiredDate.HasValue &&
                model.RequiredDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(model.RequiredDate),
                    "Required date cannot be in the past.");
            }

            if (model.CollectionMethod == "Delivery" &&
                string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                ModelState.AddModelError(
                    nameof(model.DeliveryAddress),
                    "Delivery address is required.");
            }

            if (!ModelState.IsValid)
            {
                LoadCakeInfo(model);
                return View(model);
            }

            try
            {
                int orderId = _orderService.Create(
                    customerId.Value, model);

                TempData["SuccessMessage"] =
                    "Order placed successfully!";

                return RedirectToAction(
                    nameof(Details),
                    new { id = orderId });
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(
                    string.Empty, ex.Message);

                LoadCakeInfo(model);
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to place order. Please try again.");

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
        public IActionResult UpdateStatus(
            int orderId, string status)
        {
            int? bakerId = GetCurrentUserId();

            if (!bakerId.HasValue)
                return Challenge();

            try
            {
                _orderService.UpdateStatus(
                    orderId, status, bakerId.Value);

                TempData["SuccessMessage"] =
                    "Order status updated successfully.";
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
                    "Unable to update order status.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
