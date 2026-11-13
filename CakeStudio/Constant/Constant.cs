namespace CakeStudio.Constant
{

    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Preparing = "Preparing";
        public const string Ready = "Ready";
        public const string Completed = "Completed";
        public const string Rejected = "Rejected";
        public const string AwaitingPayment = "AwaitingPayment";
    }

    public static class PaymentStatus
    {
        public const string Paid = "Paid";
        public const string Refunded = "Refunded";
        public const string Pending = "Pending";
    }

    public static class PaymentMethod
    {
        public const string Card = "Card";
    }

    public static class BakingCapacity
    {
        public const int DailyLimit = 5;
    }

}
