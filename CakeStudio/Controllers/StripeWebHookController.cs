using CakeStudio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace CakeStudio.Controllers
{
    [Route("stripe/webhook")]
    public class StripeWebhookController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IConfiguration _configuration;

        public StripeWebhookController(IOrderService orderService, IConfiguration configuration)
        {
            _orderService = orderService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Index()
        {
            var secret = _configuration["Stripe:WebhookSecret"];

            if (string.IsNullOrWhiteSpace(secret))
            {
                Console.WriteLine("Webhook: Stripe:WebhookSecret is missing.");
                return StatusCode(500);
            }

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            Event stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(body, Request.Headers["Stripe-Signature"].ToString(), secret);
            }
            catch (StripeException)
            {
                return BadRequest();
            }

            if (stripeEvent.Type == "checkout.session.completed" && stripeEvent.Data.Object is Stripe.Checkout.Session checkout && !checkout.Livemode &&  checkout.PaymentStatus == "paid" && checkout.Currency == "myr" && checkout.AmountTotal.HasValue)
            {
                try
                {
                    _orderService.ConfirmStripePayment(null, checkout.Id, checkout.AmountTotal.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Webhook: payment confirmation failed: {ex}");
                    return StatusCode(500);
                }
            }

            return Ok();
        }
    }
}