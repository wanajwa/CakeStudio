using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.Services.Interfaces;

namespace CakeStudio.Services
{
    public class NotificationService : INotificationService
    {
        private readonly NHibernateHelper _nhibernateHelper;

        public NotificationService(NHibernateHelper nhibernateHelper)
        {
            _nhibernateHelper = nhibernateHelper;
        }

        public List<Notification> GetLatestByCustomer(int customerId)
        {
            using var session = _nhibernateHelper.OpenSession();

            var notifications = session.QueryOver<Notification>()
                .Where(n => n.UserId == customerId)
                .OrderBy(n => n.CreatedAt).Desc
                .ThenBy(n => n.NotificationId).Desc
                .Take(10)
                .List();

            return new List<Notification>(notifications);
        }

        public int GetUnreadCount(int customerId)
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.QueryOver<Notification>()
                .Where(n => n.UserId == customerId)
                .And(n => n.IsRead == false)
                .RowCount();
        }

        public int MarkAsRead(int notificationId, int customerId)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();

            var notification = session.QueryOver<Notification>()
                .Where(n => n.NotificationId == notificationId)
                .And(n => n.UserId == customerId)
                .SingleOrDefault();

            if (notification == null)
            {
                throw new ArgumentException("Notification not found.");
            }

            notification.IsRead = true;

            session.Update(notification);
            transaction.Commit();
            return notification.OrderId;
        }
    }
}