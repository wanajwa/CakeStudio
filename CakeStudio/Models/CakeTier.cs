using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class CakeTier
    {
        public virtual int TierId { get; set; }

        [Required(ErrorMessage = "Tier name is required.")]
        [StringLength(100)]
        public virtual string TierName { get; set; } = string.Empty;

        [Range(0, 10000, ErrorMessage = "Additional price cannot be negative.")]
        public virtual decimal AdditionalPrice { get; set; }

        public virtual bool IsAvailable { get; set; } = true;
    }
}