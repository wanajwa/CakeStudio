
using System.ComponentModel.DataAnnotations;

namespace CakeStudio.ViewModels
{
    public class OrderViewModel
    {
        public int? CakeId { get; set; }
        public int? FlavourId { get; set; }
        public int? SizeId { get; set; }
        public int? TierId { get; set; }

        [Required(ErrorMessage = "Required date is required.")]
        [DataType(DataType.Date)]
        public DateTime? RequiredDate { get; set; }

        [Required(ErrorMessage = "Please select a pickup/delivery time.")]
        [DataType(DataType.Time)]
        public TimeSpan? RequiredTime { get; set; }


        [Required(ErrorMessage = "Collection method is required.")]
        public string CollectionMethod { get; set; } = string.Empty;

        [StringLength(500)]
        public string? DeliveryAddress { get; set; }

        public CartViewModel Cart { get; set; } = new CartViewModel();

        public int OrderId { get; set; }

        public List<CakeStudio.Models.OrderItem> OrderItems { get; set; } = new List<CakeStudio.Models.OrderItem>();

    }
}
