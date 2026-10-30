using CakeStudio.ViewModels;

namespace CakeStudio.Services
{
    public interface ICartService
    {
        CartViewModel GetCart();

        void AddItem(CartItemViewModel model);

        void UpdateItems(List<CartItemViewModel> items);
        void RemoveItem(string cartItemId);

        void ClearCart();
    }
}