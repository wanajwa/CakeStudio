using CakeStudio.Models;

namespace CakeStudio.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalCakes { get; set; }
        public int AvailableCakes { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalSales { get; set; }

        public Dictionary<string, ChartPeriodData> SalesByPeriod { get; set; } = new();

        public List<string> CakeLabels { get; set; } = new();
        public List<int> CakeOrderCounts { get; set; } = new();

        public List<Order> UpcomingOrders { get; set; } = new();
    }

    public class ChartPeriodData
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> Values { get; set; } = new();
    }
}