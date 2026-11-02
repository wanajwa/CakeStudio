using CakeStudio.Models;

namespace CakeStudio.Services.Interfaces
{
    public interface INotificationService
    {
        List<Notification> GetLatestByCustomer(int customerId);

        int GetUnreadCount(int customerId);

        int MarkAsRead(int notificationId, int customerId);
    }
}