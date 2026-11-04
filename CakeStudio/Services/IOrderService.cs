
using CakeStudio.Models;
using CakeStudio.ViewModels;

namespace CakeStudio.Services
{
    public interface IOrderService
    {
        List<Order> GetAll();

        Order? GetById(int id);

        OrderDetailsViewModel? GetOrderDetails(int id);

        OrderListViewModel GetPagedOrders(int page, int pageSize, string sort = "");

        OrderListViewModel GetPagedOrdersByCustomer(int customerId, int page, int pageSize);
        List<Payment> GetAllPayments();

        int Create(int customerId, OrderViewModel model, string paymentMethod);

        int GetBookedCakeQuantity(DateTime requiredDate,int? excludeOrderId = null);
        void UpdateStatus(int orderId, string status, int bakerId, string? rejectionReason = null);

        decimal CalculateTotalPrice(OrderViewModel model);

        void UpdateCustomerOrder(int orderId, int customerId, OrderViewModel model);

    }
}
