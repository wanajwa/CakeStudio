using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class Payment
    {
        public virtual int PaymentId { get; set; }

        [Required]
        public virtual Order Order { get; set; } = null!;

        [Range(typeof(decimal), "0.01", "100000")]
        public virtual decimal Amount { get; set; }
        [Required]
        [StringLength(50)]
        public virtual string PaymentMethod { get; set; } = string.Empty;

        [Required(ErrorMessage = "Payment status is required.")]
        [StringLength(50)]
        public virtual string PaymentStatus { get; set; } = string.Empty;

        public virtual DateTime? PaidAt { get; set; }
        public virtual DateTime? RefundedAt { get; set; }
    }
}