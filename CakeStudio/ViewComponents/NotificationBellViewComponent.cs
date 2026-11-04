using CakeStudio.Services.Interfaces;
using CakeStudio.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CakeStudio.ViewComponents
{
    public class NotificationBellViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationBellViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public IViewComponentResult Invoke()
        {
            var user = HttpContext.User;

            if (!user.IsInRole("Customer"))
                return Content("");

            string? value =
                user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out int customerId))
                return Content("");

            var model = new NotificationViewModel
            {
                Notifications = _notificationService
                    .GetLatestByCustomer(customerId),

                UnreadCount = _notificationService
                    .GetUnreadCount(customerId)
            };

            return View(model);
        }
    }
}