using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class CakeSize
    {
        public virtual int SizeId { get; set; }

        [Required(ErrorMessage = "Size name is required.")]
        [StringLength(100)]
        public virtual string SizeName { get; set; } = string.Empty;

        [Range(0, 10000, ErrorMessage = "Additional price cannot be negative.")]
        public virtual decimal AdditionalPrice { get; set; }

        public virtual bool IsAvailable { get; set; } = true;
    }
}