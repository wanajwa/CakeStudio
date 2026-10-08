using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class Payment
    {
        public virtual int PaymentId { get; set; }

        [Required]
        public virtual int? OrderId { get; set; }

        [Range(0, 100000, ErrorMessage = "Payment amount cannot be negative.")]
        public virtual decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment status is required.")]
        [StringLength(50)]
        public virtual string PaymentStatus { get; set; } = "Pending";
    }
}