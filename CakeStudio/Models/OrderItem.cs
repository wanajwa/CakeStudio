namespace CakeStudio.Models
{
    public class OrderItem
    {
        public virtual int OrderItemId { get; set; }

        public virtual int OrderId { get; set; }

        public virtual int CakeId { get; set; }

        public virtual int? FlavourId { get; set; }

        public virtual int? SizeId { get; set; }

        public virtual int? TierId { get; set; }

        public virtual int Quantity { get; set; } = 1;

        public virtual decimal UnitPrice { get; set; }

        public virtual string? MessageOnCakes { get; set; }

        // Navigation properties
        public virtual Order? Order { get; set; }

        public virtual Cake? Cake { get; set; }

        public virtual CakeFlavour? Flavour { get; set; }

        public virtual CakeSize? Size { get; set; }

        public virtual CakeTier? Tier { get; set; }
    }
}