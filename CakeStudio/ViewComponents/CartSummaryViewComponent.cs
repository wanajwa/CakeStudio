using CakeStudio.Services;
using Microsoft.AspNetCore.Mvc;

namespace CakeStudio.ViewComponents
{
    public class CartSummaryViewComponent : ViewComponent
    {
        private readonly ICartService _cartService;

        public CartSummaryViewComponent(ICartService cartService)
        {
            _cartService = cartService;
        }

        public IViewComponentResult Invoke()
        {
            if (!HttpContext.User.Identity!.IsAuthenticated || !HttpContext.User.IsInRole("Customer"))
            {
                return Content("");
            }

            var quantity = _cartService.GetCart().TotalQuantity;

            return View(quantity);
        }
    }
}