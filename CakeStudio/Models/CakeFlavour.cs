using System.ComponentModel.DataAnnotations;

namespace CakeStudio.Models
{
    public class CakeFlavour
    {
        public virtual int FlavourId { get; set; }

        [Required(ErrorMessage = "Flavour name is required.")]
        [StringLength(100)]
        public virtual string FlavourName { get; set; } = string.Empty;

        [Range(0, 10000, ErrorMessage = "Additional price cannot be negative.")]
        public virtual decimal AdditionalPrice { get; set; }

        public virtual bool IsAvailable { get; set; } = true;
    }
}