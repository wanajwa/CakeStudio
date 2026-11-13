using CakeStudio.Services;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CakeStudio.Resources;
using System.Security.Claims;
using System.Text.Json;
namespace CakeStudio.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ICakeSizeService _cakeSizeService;
        private readonly ICakeFlavourService _cakeFlavourService;
        private readonly ICakeTierService _cakeTierService;
        private readonly IOrderService _orderService;
        private readonly ICakeService _cakeService;
        public CartController(ICartService cartService, ICakeSizeService sizeService, ICakeFlavourService flavourService, ICakeTierService tierService, IOrderService orderService, ICakeService cakeService)
        {
            _cartService = cartService;
            _cakeSizeService = sizeService;
            _cakeFlavourService = flavourService;
            _cakeTierService = tierService;
            _orderService = orderService;
            _cakeService = cakeService;
        }

        // Display cart
        [HttpGet]
        public IActionResult Index(int? orderId)
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(customerId, out var paidCartCustomerId))
                _cartService.RemovePaidCheckoutItems(paidCartCustomerId);

            var model = _cartService.GetCart();

            ViewBag.Sizes = _cakeSizeService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            ViewBag.Flavours = _cakeFlavourService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            ViewBag.Tiers = _cakeTierService.GetAll()
                .Where(x => x.IsAvailable)
                .ToList();

            var cakes = _cakeService.GetAll();

            ViewBag.DefaultFlavourIds = cakes.ToDictionary(
                cake => cake.CakeId,
                cake => cake.Flavour.FlavourId);

            ViewBag.CakeIdsWithImages = cakes
                .Where(cake => cake.IsAvailable && cake.Image?.ImageContent?.Length > 0)
                .Select(cake => cake.CakeId)
                .ToHashSet();

            if (int.TryParse(customerId, out var id))
            {
                var draftJson = HttpContext.Session.GetString($"CheckoutDraft_{id}");

                if (!string.IsNullOrEmpty(draftJson))
                {
                    ViewBag.CheckoutDraft = JsonSerializer.Deserialize<OrderViewModel>(draftJson);
                }
            }

            ViewBag.AwaitingPaymentOrderId = orderId;

            return View(model);
        }

        // Calculate a cart item's price for the selected options
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CalculatePrice(string cartItemId,int sizeId,int flavourId,int tierId)
        {
            var cart = _cartService.GetCart();

            var item = cart.Items.FirstOrDefault(
                x => x.CartItemId == cartItemId);

            if (item == null)
                return BadRequest(new { message = Resource.CartItemNotFound });

            try
            {
                decimal unitPrice = _orderService.CalculateTotalPrice(
                    new OrderViewModel
                    {
                        CakeId = item.CakeId,
                        SizeId = sizeId,
                        FlavourId = flavourId,
                        TierId = tierId
                    });

                return Json(new { unitPrice });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Add selected cake to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(CartItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = Resource.InvalidCartOptions
                });
            }

            try
            {
                _cartService.AddItem(model);

                return Json(new
                {
                    message = Resource.CartItemAddedSuccess,
                    totalQuantity = _cartService.GetCart().TotalQuantity
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // Remove one cart entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(string cartItemId)
        {
            try
            {
                _cartService.RemoveItem(cartItemId);
                if (!_cartService.GetCart().Items.Any())
                {
                    ClearCheckoutDraft();
                }

                TempData["SuccessMessage"] = Resource.CartItemRemovedSuccess;
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            _cartService.ClearCart();
            ClearCheckoutDraft();

            TempData["SuccessMessage"] = "Cart cleared successfully.";

            return RedirectToAction(nameof(Index));
        }

        private void ClearCheckoutDraft()
        {
            var customerId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(customerId, out var id))
            {
                HttpContext.Session.Remove($"CheckoutDraft_{id}");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveChanges(List<CartItemViewModel> items)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = Resource.InvalidCartOptions;
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _cartService.UpdateItems(items);
                return RedirectToAction("Index", "CustomerCake");
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}