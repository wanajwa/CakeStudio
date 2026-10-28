using CakeStudio.Models;

namespace CakeStudio.ViewModels
{
    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; } = new();

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int FilteredOrderCount { get; set; }

        public string Sort { get; set; } = "";

        // Manage Orders summary cards
        public int TotalOrders { get; set; }
        public int PreparingOrders { get; set; }
        public int ReadyOrders { get; set; }
        public int CompletedOrders { get; set; }
    }
}
