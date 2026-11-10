using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.Resources;
using CakeStudio.ViewModels;
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

        private HttpContext CurrentContext =>_httpContextAccessor.HttpContext?? throw new InvalidOperationException(Resource.HttpContextUnavailable);

        // Separate the cart for each logged-in customer
        private string CartKey
        {
            get
            {
                var customerId = CurrentContext.User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(customerId) || !CurrentContext.User.IsInRole("Customer"))
                {
                    throw new UnauthorizedAccessException(Resource.CustomerLoginRequired);
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

            var customerId = CurrentContext.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            CurrentContext.Session.Remove($"PendingOrder_{customerId}");
        }

        public void AddItem(CartItemViewModel model)
        {
            ValidateQuantity(model.Quantity);

            ValidateCakeMessage(model.MessageOnCakes);

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
                throw new ArgumentException(Resource.CartOptionsNotFound);
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
                MessageOnCakes = string.IsNullOrWhiteSpace(model.MessageOnCakes)
                    ? null
                    : model.MessageOnCakes.Trim(),

                CakeName = cake.CakeName,
                BasePrice = cake.BasePrice,
                SizeName = size.SizeName,
                FlavourName = flavour.FlavourName,
                TierName = tier.TierName,
                SizePrice = size.SizeName.Trim().Equals("6 inch", StringComparison.OrdinalIgnoreCase)
                    ? 0m
                    : size.AdditionalPrice,

                            FlavourPrice = flavour.FlavourId == cake.Flavour.FlavourId
                ? 0m
                : flavour.AdditionalPrice,

                            TierPrice = tier.TierName.Trim().Equals("1", StringComparison.OrdinalIgnoreCase)
                    ? 0m
                    : tier.AdditionalPrice,

                UnitPrice = unitPrice
            };

            cart.Items.Add(item);
            SaveCart(cart);
        }

        public void UpdateItems(List<CartItemViewModel> items)
        {
            var cart = GetCart();

            if (items == null || items.Count != cart.Items.Count)
                throw new ArgumentException(Resource.CartChanged);

            using var session = _nhibernateHelper.OpenSession();

            var checkedIds = new HashSet<string>();
            var updatedItems = new List<CartItemViewModel>();

            // Validate and prepare all changes before saving
            foreach (var submittedItem in items)
            {
                var savedItem = cart.Items.FirstOrDefault(
                    x => x.CartItemId == submittedItem.CartItemId);

                if (savedItem == null ||
                    !checkedIds.Add(submittedItem.CartItemId))
                {
                    throw new ArgumentException(Resource.InvalidCartItem);
                }

                ValidateQuantity(submittedItem.Quantity);
                ValidateCakeMessage(submittedItem.MessageOnCakes);

                var size = session.Get<CakeSize>(submittedItem.SizeId);
                var flavour = session.Get<CakeFlavour>(submittedItem.FlavourId);
                var tier = session.Get<CakeTier>(submittedItem.TierId);

                if (size == null || flavour == null || tier == null ||
                    !size.IsAvailable || !flavour.IsAvailable || !tier.IsAvailable)
                {
                    throw new ArgumentException(Resource.CartOptionsNotFound);
                }

                var cake = session.Get<Cake>(savedItem.CakeId);

                if (cake == null || !cake.IsAvailable)
                    throw new ArgumentException(Resource.CakeUnavailable);

                // Keep the original cake and calculate its new option price
                decimal unitPrice = _orderService.CalculateTotalPrice(
                    new OrderViewModel
                    {
                        CakeId = savedItem.CakeId,
                        SizeId = size.SizeId,
                        FlavourId = flavour.FlavourId,
                        TierId = tier.TierId
                    });

                updatedItems.Add(new CartItemViewModel
                {
                    CartItemId = savedItem.CartItemId,
                    CakeId = savedItem.CakeId,
                    CakeName = savedItem.CakeName,
                    BasePrice = cake.BasePrice,

                    SizeId = size.SizeId,
                    SizeName = size.SizeName,
                    FlavourId = flavour.FlavourId,
                    FlavourName = flavour.FlavourName,
                    TierId = tier.TierId,
                    TierName = tier.TierName,

                    Quantity = submittedItem.Quantity,
                    SizePrice = size.SizeName.Trim().Equals(
                        "6 inch", StringComparison.OrdinalIgnoreCase)
                            ? 0m
                            : size.AdditionalPrice,

                                        FlavourPrice = flavour.FlavourId == cake.Flavour.FlavourId
                        ? 0m
                        : flavour.AdditionalPrice,

                                        TierPrice = tier.TierName.Trim().Equals(
                        "1", StringComparison.OrdinalIgnoreCase)
                            ? 0m
                            : tier.AdditionalPrice,

                    UnitPrice = unitPrice,
                    MessageOnCakes =
                        string.IsNullOrWhiteSpace(submittedItem.MessageOnCakes)
                            ? null
                            : submittedItem.MessageOnCakes.Trim()
                });
            }

            cart.Items.Clear();
            cart.Items.AddRange(updatedItems);

            SaveCart(cart);
        }

        public void RemoveItem(string cartItemId)
        {
            var cart = GetCart();

            var item = cart.Items.Find(x => x.CartItemId == cartItemId);

            if (item == null)
                throw new ArgumentException(Resource.CartItemNotFound);

            cart.Items.Remove(item);

            SaveCart(cart);
        }

        public void ClearCart()
        {
            CurrentContext.Session.Remove(CartKey);
            var customerId = CurrentContext.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            CurrentContext.Session.Remove($"PendingOrder_{customerId}");
        }

        private static void ValidateQuantity(int quantity)
        {
            if (quantity < 1 || quantity > 20)
            {
                throw new ArgumentException(Resource.InvalidCakeQuantity);
            }
        }
        private static void ValidateCakeMessage(string? message)
        {
            if (message?.Length > 500)
            {
                throw new ArgumentException(Resource.CakeMessageTooLong);
            }
        }
    }
}