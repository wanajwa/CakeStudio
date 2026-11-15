using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class Order
    {
        public virtual int OrderId { get; set; }
        public virtual int? CustId { get; set; }
        public virtual int? BakerId { get; set; }
        public virtual string? RejectionReason { get; set; }

        [Required(ErrorMessage = "Required date is required.")]
        [DataType(DataType.Date)]
        public virtual DateTime? RequiredDate { get; set; }

        [Range(typeof(decimal), "0.01", "100000", ErrorMessage = "Total price must be greater than RM0.")]
        public virtual decimal TotalPrice { get; set; }

        [Required]
        [StringLength(50)]
        public virtual string OrderStatus { get; set; } = CakeStudio.Constant.OrderStatus.OrderPlaced;

        public virtual DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual DateTime? UpdatedAt { get; set; }

        public virtual int? UpdatedId { get; set; }

        public virtual int? CreatedId { get; set; }


        // Navigation properties
        public virtual User? Customer { get; set; }

        public virtual IList<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    }
}