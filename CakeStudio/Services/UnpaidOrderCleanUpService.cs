using CakeStudio.Constant;
using CakeStudio.Data;
using CakeStudio.Resources;
using Stripe;

namespace CakeStudio.Services;

public sealed class UnpaidOrderCleanupService(IServiceScopeFactory scopeFactory,IConfiguration configuration,StripeClient stripeClient,ILogger<UnpaidOrderCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var cutoff = DateTime.Now.AddMinutes(-5);

                foreach (var (orderId, customerId) in orders.GetExpiredUnpaidOrders(cutoff))
                {
                    try
                    {
                        await CheckoutLock.RunAsync(configuration,customerId,
                            () => ProcessOrderAsync(orders, orderId, customerId, cutoff, stoppingToken),
                            () => false,stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning(ex, "Could not check unpaid order {OrderId}.", orderId);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Could not scan unpaid orders.");
            }
        }
    }

    private async Task<bool> ProcessOrderAsync(IOrderService orders,int orderId,int customerId,DateTime cutoff,CancellationToken cancellationToken)
    {
        var details = orders.GetOrderDetails(orderId);
        var payment = details?.Payment;

        if (details == null || details.Order.CustId != customerId || details.Order.OrderStatus != OrderStatus.PendingPayment || details.Order.CreatedAt > cutoff || payment == null || payment.PaymentStatus != PaymentStatus.Pending) 
        {
            return false;
        }

        var sessionId = payment.StripeCheckoutSessionId;

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var sessions = stripeClient.V1.Checkout.Sessions;
            var session = await sessions.GetAsync(sessionId, cancellationToken: cancellationToken);

            if (session.PaymentStatus == "paid")
            {
                if (session.Livemode || session.Currency != "myr" || !session.AmountTotal.HasValue)
                {
                    throw new InvalidOperationException(Resource.StripePaymentNotVerified);
                }

                orders.ConfirmStripePayment(customerId, session.Id, session.AmountTotal.Value);

                return true;
            }

            if (session.Status == "open" && session.PaymentStatus == "unpaid")
            {
                session = await sessions.ExpireAsync(sessionId, cancellationToken: cancellationToken);
            }

            if (session.Status != "expired" || session.PaymentStatus != "unpaid")
                return false;
        }

        orders.CancelUnpaidOrder(orderId,customerId,sessionId, Resource.PaymentNotCompleted, automatic: true);

        return true;
    }
}