using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace CakeStudio.Services
{
    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly NHibernateHelper _nhibernateHelper;
        private readonly IOrderService _orderService;

        public CartService(IHttpContextAccessor httpContextAccessor, NHibernateHelper nhibernateHelper, IOrderService orderService)
        {
            _httpContextAccessor = httpContextAccessor;
            _nhibernateHelper = nhibernateHelper;
            _orderService = orderService;
        }

        private HttpContext CurrentContext =>_httpContextAccessor.HttpContext?? throw new InvalidOperationException("HTTP context is unavailable.");

        // Separate the cart for each logged-in customer
        private string CartKey
        {
            get
            {
                var customerId = CurrentContext.User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(customerId) || !CurrentContext.User.IsInRole("Customer"))
                {
                    throw new UnauthorizedAccessException("Please log in as a customer.");
                }

                return $"Cart_{customerId}";
            }
        }

        public CartViewModel GetCart()
        {
            var json = CurrentContext.Session.GetString(CartKey);

            if (string.IsNullOrEmpty(json))
                return new CartViewModel();

            return JsonSerializer.Deserialize<CartViewModel>(json)
                ?? new CartViewModel();
        }

        private void SaveCart(CartViewModel cart)
        {
            CurrentContext.Session.SetString(CartKey,JsonSerializer.Serialize(cart));
        }

        public void AddItem(CartItemViewModel model)
        {
            ValidateQuantity(model.Quantity);

            if (model.MessageOnCakes?.Length > 500)
            {
                throw new ArgumentException("Cake message must not exceed 500 characters.");
            }

            // Calculate the price using database values
            decimal unitPrice = _orderService.CalculateTotalPrice(
                new OrderViewModel
                {
                    CakeId = model.CakeId,
                    SizeId = model.SizeId,
                    FlavourId = model.FlavourId,
                    TierId = model.TierId
                });

            using var session = _nhibernateHelper.OpenSession();

            var cake = session.Get<Cake>(model.CakeId);
            var size = session.Get<CakeSize>(model.SizeId);
            var flavour = session.Get<CakeFlavour>(model.FlavourId);
            var tier = session.Get<CakeTier>(model.TierId);

            if (cake == null || size == null || flavour == null || tier == null)
            {
                throw new ArgumentException("The selected cake or options could not be found.");
            }

            var cart = GetCart();

            // Create a new entry using trusted database information
            var item = new CartItemViewModel
            {
                CakeId = cake.CakeId,
                SizeId = size.SizeId,
                FlavourId = flavour.FlavourId,
                TierId = tier.TierId,
                Quantity = model.Quantity,
                MessageOnCakes = model.MessageOnCakes?.Trim(),

                CakeName = cake.CakeName,
                SizeName = size.SizeName,
                FlavourName = flavour.FlavourName,
                TierName = tier.TierName,
                UnitPrice = unitPrice
            };

            cart.Items.Add(item);
            SaveCart(cart);
        }

        public void UpdateItems(List<CartItemViewModel> items)
        {
            var cart = GetCart();

            if (items == null || items.Count != cart.Items.Count)
                throw new ArgumentException("Your cart has changed. Please review it.");

            var checkedIds = new HashSet<string>();

            // Validate every item before changing the cart
            foreach (var submittedItem in items)
            {
                var savedItem = cart.Items.FirstOrDefault(x => x.CartItemId == submittedItem.CartItemId);

                if (savedItem == null || !checkedIds.Add(submittedItem.CartItemId))
                {
                    throw new ArgumentException("Invalid cart item.");
                }

                if (submittedItem.Quantity < 1 || submittedItem.Quantity > 20)
                    throw new ArgumentException("Quantity must be between 1 and 20.");

                if (submittedItem.MessageOnCakes?.Length > 500)
                {
                    throw new ArgumentException("Cake message must not exceed 500 characters.");
                }
            }

            // Update only quantities and messages
            foreach (var submittedItem in items)
            {
                var savedItem = cart.Items.First(x => x.CartItemId == submittedItem.CartItemId);

                savedItem.Quantity = submittedItem.Quantity;

                savedItem.MessageOnCakes = string.IsNullOrWhiteSpace(submittedItem.MessageOnCakes)
                        ? null
                        : submittedItem.MessageOnCakes.Trim();
            }

            SaveCart(cart);
        }

        public void RemoveItem(string cartItemId)
        {
            var cart = GetCart();

            var item = cart.Items.Find(x => x.CartItemId == cartItemId);

            if (item == null)
                throw new ArgumentException("Cart item not found.");

            cart.Items.Remove(item);

            SaveCart(cart);
        }

        public void ClearCart()
        {
            CurrentContext.Session.Remove(CartKey);
        }

        private static void ValidateQuantity(int quantity)
        {
            if (quantity < 1 || quantity > 20)
            {
                throw new ArgumentException("Quantity must be between 1 and 20.");
            }
        }
    }
}