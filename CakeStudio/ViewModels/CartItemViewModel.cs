namespace CakeStudio.ViewModels
{
    public class CartItemViewModel
    {
        public string CartItemId { get; set; } = Guid.NewGuid().ToString();

        public int CakeId { get; set; }

        public int SizeId { get; set; }

        public int FlavourId { get; set; }

        public int TierId { get; set; }

        public int Quantity { get; set; } = 1;

        public string? MessageOnCakes { get; set; }

        // Display information
        public string CakeName { get; set; } = string.Empty;

        public string SizeName { get; set; } = string.Empty;

        public string FlavourName { get; set; } = string.Empty;

        public string TierName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;
    }
}