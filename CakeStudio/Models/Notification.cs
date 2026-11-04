namespace CakeStudio.Models
{
    public class Notification
    {
        public virtual int NotificationId { get; set; }

        public virtual int UserId { get; set; }

        public virtual int OrderId { get; set; }

        public virtual string Message { get; set; } = "";

        public virtual bool IsRead { get; set; }

        public virtual DateTime? ReadAt { get; set; }
        public virtual DateTime CreatedAt { get; set; }
    }
}