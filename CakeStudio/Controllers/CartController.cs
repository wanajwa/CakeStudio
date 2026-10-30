using CakeStudio.Services;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CakeStudio.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // Display cart
        [HttpGet]
        public IActionResult Index()
        {
            var model = _cartService.GetCart();

            return View(model);
        }

        // Add selected cake to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(CartItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please enter valid cake options and quantity.";

                return RedirectToAction("Index", "CustomerCake");
            }

            try
            {
                _cartService.AddItem(model);

                TempData["SuccessMessage"] = "Cake added to cart.";

                return RedirectToAction(nameof(Index));
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                return RedirectToAction("Index", "CustomerCake");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Checkout(List<CartItemViewModel> items)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please check your cart information.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _cartService.UpdateItems(items);

                return RedirectToAction("Checkout", "Order");
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;

                return RedirectToAction(nameof(Index));
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

                TempData["SuccessMessage"] = "Item removed from cart.";
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}