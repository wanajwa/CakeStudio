
using CakeStudio.Models;
using CakeStudio.ViewModels;

namespace CakeStudio.Services
{
    public interface IOrderService
    {
        List<Order> GetAll();

        Order? GetById(int id);

        OrderDetailsViewModel? GetOrderDetails(int id);

        List<Order> GetOrdersByCustomer(int customerId);

        OrderListViewModel GetPagedOrders(int page = 1, int pageSize = 5, string sort = "");

        OrderListViewModel GetPagedOrdersByCustomer(int customerId, int page, int pageSize);
        List<Payment> GetAllPayments();

        int Create(int customerId, OrderViewModel model, string paymentMethod);

        void UpdateStatus(int orderId, string status, int bakerId);

        decimal CalculateTotalPrice(OrderViewModel model);

    }
}
