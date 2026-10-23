
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
        List<Payment> GetAllPayments();

        int Create(int customerId, OrderViewModel model, string paymentMethod);

        void UpdateStatus(int orderId, string status, int bakerId);

        decimal CalculateTotalPrice(OrderViewModel model);

    }
}
