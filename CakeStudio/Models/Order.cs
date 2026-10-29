using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class Order
    {
        public virtual int OrderId { get; set; }
        public virtual int? CustId { get; set; }
        [Required(ErrorMessage = "Please select a cake.")]
        public virtual int? CakeId { get; set; }

        public virtual int? FlavourId { get; set; }

        public virtual int? SizeId { get; set; }

        public virtual int? TierId { get; set; }

        public virtual int? BakerId { get; set; }

        [Required(ErrorMessage = "Required date is required.")]
        [DataType(DataType.Date)]
        public virtual DateTime? RequiredDate { get; set; }

        [Required(ErrorMessage = "Collection method is required.")]
        [StringLength(50)]
        public virtual string CollectionMethod { get; set; } = string.Empty;

        [StringLength(500)]
        public virtual string? DeliveryAddress { get; set; }

        [StringLength(500)]
        public virtual string? MessageOnCakes { get; set; }

        [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "Total price must be greater than RM0.")]
        public virtual decimal TotalPrice { get; set; }

        [Required]
        [StringLength(50)]
        public virtual string OrderStatus { get; set; } = "Pending";

        public virtual DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual DateTime? UpdatedAt { get; set; }

        public virtual int? UpdatedId { get; set; }

        public virtual int? CreatedId { get; set; }


        // Navigation properties
        public virtual User? Customer { get; set; }

        public virtual Cake? Cake { get; set; }

        public virtual CakeSize? Size { get; set; }

        public virtual CakeFlavour? Flavour { get; set; }

        public virtual CakeTier? Tier { get; set; }

        public virtual IList<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }
}