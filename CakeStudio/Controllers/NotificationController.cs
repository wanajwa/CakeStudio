using CakeStudio.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CakeStudio.Controllers
{
    [Authorize(Roles = "Customer")]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Open(int notificationId)
        {
            string? value =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(value, out int customerId))
                return Challenge();

            try
            {
                int orderId = _notificationService.MarkAsRead(
                    notificationId, customerId);

                return RedirectToAction(
                    "Details", "Order", new { id = orderId });
            }
            catch (ArgumentException)
            {
                return NotFound();
            }
        }
    }
}