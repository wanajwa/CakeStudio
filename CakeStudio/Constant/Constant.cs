namespace CakeStudio.Constant
{

    public static class OrderStatus
    {
        public const string OrderPlaced = "OrderPlaced";
        public const string Preparing = "Preparing";
        public const string Ready = "Ready";
        public const string Completed = "Completed";
        public const string Rejected = "Rejected";
        public const string PendingPayment = "PendingPayment";
        public const string Cancelled = "Cancelled";
    }

    public static class PaymentStatus
    {
        public const string Paid = "Paid";
        public const string Refunded = "Refunded";
        public const string Pending = "Pending";
        public const string PaymentCancelled = "PaymentCancelled";
        public const string RefundRequired = "RefundRequired";
    }

    public static class PaymentMethod
    {
        public const string Card = "Card";
    }

    public static class BakingCapacity
    {
        public const int DailyLimit = 5;
    }

    public static class PickupHours
    {
        public static readonly TimeSpan OpensAt = TimeSpan.FromHours(8);
        public static readonly TimeSpan ClosesAt = TimeSpan.FromHours(17);

        public static bool IsValid(TimeSpan time)
        {
            return time >= OpensAt && time <= ClosesAt;
        }
    }
}
