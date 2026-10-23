
using CakeStudio.Models;

namespace CakeStudio.ViewModels
{
    public class OrderDetailsViewModel
    {
        //hold result from service tadi
        public Order Order { get; set; } = null!;

        public string CakeName { get; set; } = "";
        public string FlavourName { get; set; } = "";
        public string SizeName { get; set; } = "";
        public string TierName { get; set; } = "";

        public Payment? Payment { get; set; }
    }
}
