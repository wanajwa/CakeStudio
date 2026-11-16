
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

        int? FindCheckoutOrderId(int customerId, Guid checkoutRequestId);
        List<Payment> GetAllPayments();

        int CreateAwaitingPayment(int customerId, OrderViewModel model, int? existingOrderId = null, Guid? checkoutRequestId = null);

        void SaveStripeSession(int orderId, int customerId, string stripeSessionId);

        void ClearStripeSession(int orderId, int customerId, string stripeSessionId);

        void CancelUnpaidOrder(int orderId, int customerId, string? stripeSessionId, string reason, bool automatic = false);

        IReadOnlyList<(int OrderId, int CustomerId)> GetExpiredUnpaidOrders(DateTime cutoff);

        int ConfirmStripePayment(int? customerId, string stripeSessionId, long amountInSen);

        int Create(int customerId, OrderViewModel model, string paymentMethod, string? stripeCheckoutSessionId = null, long? stripeAmountInSen = null);

        void UpdateStatus(int orderId, string status, int bakerId, string? rejectionReason = null);

        decimal CalculateTotalPrice(OrderViewModel model);

        void UpdateCustomerOrder(int orderId, int customerId, OrderViewModel model);

        
    }
}
