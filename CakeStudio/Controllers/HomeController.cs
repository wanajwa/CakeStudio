using System.Diagnostics;
using CakeStudio.Models;
using Microsoft.AspNetCore.Mvc;
using CakeStudio.Services;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Authorization;
using CakeStudio.Constant;

namespace CakeStudio.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ICakeService _cakeService;
        private readonly IOrderService _orderService;

        public HomeController(ILogger<HomeController> logger, ICakeService cakeService, IOrderService orderService)
        {
            _logger = logger;
            _cakeService = cakeService;
            _orderService = orderService;
        }


        [HttpGet]
        [Authorize(Roles = "Baker")]
        public IActionResult Index()
        {
            var cakes = _cakeService.GetAll();
            var orders = _orderService.GetAll();

            var today = DateTime.Today;

            // Retrieve successful, non-refunded payments
            var payments = _orderService.GetAllPayments();

            var paidPayments = payments
                .Where(p => p.PaymentStatus == PaymentStatus.Paid)
                .ToList();


            var salesByPeriod = new Dictionary<string, ChartPeriodData>();

            // Daily - seminggu
            var dailyDates = Enumerable.Range(0, 7)
                .Select(i => today.AddDays(i - 6))
                .ToList();

            salesByPeriod["Daily"] = new ChartPeriodData
            {
                Labels = dailyDates
                    .Select(d => d.ToString("dd MMM"))
                    .ToList(),

                Values = dailyDates
                    .Select(d => paidPayments
                        .Where(p => p.PaidAt.HasValue && p.PaidAt.Value.Date == d)
                        .Sum(p => p.Amount))
                    .ToList()
            };

            // Weekly - last 8 weeks
            var currentMonday = today.AddDays(
                -((7 + (int)today.DayOfWeek -
                   (int)DayOfWeek.Monday) % 7));

            var weekStarts = Enumerable.Range(0, 8)
                .Select(i => currentMonday.AddDays((i - 7) * 7))
                .ToList();

            salesByPeriod["Weekly"] = new ChartPeriodData
            {
                Labels = weekStarts
                    .Select(d => d.ToString("dd MMM"))
                    .ToList(),

                Values = weekStarts
                    .Select(start => paidPayments
                        .Where(p => p.PaidAt.HasValue && p.PaidAt.Value >= start && p.PaidAt.Value < start.AddDays(7))
                        .Sum(p => p.Amount))
                    .ToList()
            };

            // Monthly - current year
            var months = Enumerable.Range(1, 12).ToList();

            salesByPeriod["Monthly"] = new ChartPeriodData
            {
                Labels = months
                    .Select(m => new DateTime(today.Year, m, 1)
                        .ToString("MMM"))
                    .ToList(),

                Values = months
                    .Select(m => paidPayments
                        .Where(p => p.PaidAt.HasValue && p.PaidAt.Value.Year == today.Year &&
                                    p.PaidAt.Value.Month == m)
                        .Sum(p => p.Amount))
                    .ToList()
            };

            // Yearly - last 5 years
            var years = Enumerable.Range(today.Year - 4, 5).ToList();

            salesByPeriod["Yearly"] = new ChartPeriodData
            {
                Labels = years
                    .Select(y => y.ToString())
                    .ToList(),

                Values = years
                    .Select(y => paidPayments
                        .Where(p => p.PaidAt.HasValue && p.PaidAt.Value.Year == y)
                        .Sum(p => p.Amount))
                    .ToList()
            };


            // Top 5 most ordered cakes
            var popularCakes = orders
                .Where(o => o.OrderStatus != OrderStatus.Rejected)
                .SelectMany(o => o.OrderItems)
                .Where(item => item.Cake != null)
                .GroupBy(item => new
                {
                    item.CakeId,
                    item.Cake!.CakeName
                })
                .Select(g => new
                {
                    CakeName = g.Key.CakeName,
                    Count = g.Sum(item => item.Quantity)
                })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            // Upcoming orders - next 5 orders
            var upcomingOrders = orders
                .Where(o => o.RequiredDate.HasValue &&
                            o.RequiredDate.Value >= today &&
                            o.OrderStatus != "Completed" &&
                            o.OrderStatus != "Rejected")
                .OrderBy(o => o.RequiredDate)
                .Take(5)
                .ToList();

            var model = new DashboardViewModel
            {
                TotalCakes = cakes.Count,

                AvailableCakes = cakes.Count(c => c.IsAvailable),

                PendingOrders = orders.Count(o => o.OrderStatus == "Pending"),

                TotalSales = paidPayments.Sum(p => p.Amount),

                SalesByPeriod = salesByPeriod,

                CakeLabels = popularCakes
                .Select(x => x.CakeName)
                .ToList(),

                CakeOrderCounts = popularCakes
                .Select(x => x.Count)
                .ToList(),

                UpcomingOrders = upcomingOrders
            };

            return View(model);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
