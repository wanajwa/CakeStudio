
using CakeStudio.Models;

namespace CakeStudio.ViewModels
{
    public class OrderDetailsViewModel
    {
        //hold result from service tadi
        public Order Order { get; set; } = null!;
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public Payment? Payment { get; set; }

        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
    }
}
