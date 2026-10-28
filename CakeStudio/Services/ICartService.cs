using CakeStudio.ViewModels;

namespace CakeStudio.Services
{
    public interface ICartService
    {
        CartViewModel GetCart();

        void AddItem(CartItemViewModel model);

        void UpdateQuantity(string cartItemId, int quantity);

        void RemoveItem(string cartItemId);

        void ClearCart();
    }
}