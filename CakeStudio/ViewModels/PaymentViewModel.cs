using System.ComponentModel.DataAnnotations;
namespace CakeStudio.ViewModels
{
    public class PaymentViewModel
    {
        public decimal Amount {  get; set; }
        public string PaymentMethod { get; set; } = "Card";

        [Required(ErrorMessage = "Card number is required.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required.")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "Security code is required.")]
        public string SecurityCode { get; set; } = string.Empty;

        public string CakeName { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }

        public string SizeName { get; set; } = string.Empty;
        public decimal SizePrice { get; set; }

        public string FlavourName { get; set; } = string.Empty;
        public decimal FlavourPrice { get; set; }

        public string TierName { get; set; } = string.Empty;
        public decimal TierPrice { get; set; }

        public DateTime? RequiredDate { get; set; }
        public TimeSpan? RequiredTime { get; set; }

        public string CollectionMethod { get; set; } = string.Empty;
        public CartViewModel Cart { get; set; } = new CartViewModel();
    }
}
