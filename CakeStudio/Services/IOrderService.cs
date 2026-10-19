
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

        int Create(int customerId, OrderViewModel model);

        void UpdateStatus(
            int orderId,
            string status,
            int bakerId
        );

       
    }
}
