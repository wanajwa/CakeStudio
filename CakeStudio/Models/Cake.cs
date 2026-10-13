using System.ComponentModel.DataAnnotations; // allow validate attrbute

namespace CakeStudio.Models
{
    public class Cake
    {

        // [reuired], [string] [range] == Data annotations
        public virtual int CakeId { get; set;}

        [Required(ErrorMessage = "Cake name is required.")] // cannot be null or empty
        [StringLength(100)]
        public virtual string CakeName { get; set; } = string.Empty;

        [StringLength(500)]
        public virtual string? Description { get; set; }

        [Range(0.01, 10000, ErrorMessage = "Base price must be greater than RM0.")]
        public virtual decimal BasePrice { get; set; }

        public virtual Image? Image { get; set;  }
        public virtual bool IsAvailable { get; set; } = true;
        public virtual CakeFlavour Flavour { get; set; } = null!;
    }
}