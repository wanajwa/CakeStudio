using CakeStudio.Models;

namespace CakeStudio.ViewModels
{
    public class NotificationViewModel
    {
        public List<Notification> Notifications { get; set; } = new();

        public int UnreadCount { get; set; }
    }
}