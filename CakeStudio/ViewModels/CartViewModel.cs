namespace CakeStudio.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; }
            = new List<CartItemViewModel>();

        public int TotalQuantity => Items.Sum(item => item.Quantity);

        public decimal TotalPrice => Items.Sum(item => item.Subtotal);
    }
}