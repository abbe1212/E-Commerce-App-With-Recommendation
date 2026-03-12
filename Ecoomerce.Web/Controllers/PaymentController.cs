using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecoomerce.Web.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IOrderService orderService, IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _orderService = orderService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Index(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderDetailsAsync(orderId);
                if (order == null) return NotFound();

                ViewBag.OrderId = orderId;
                ViewBag.StripePublishableKey = _configuration["Stripe:PublishableKey"];
                ViewBag.TotalAmount = order.TotalAmount;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment page for order {OrderId}", orderId);
                return BadRequest("Unable to load payment page");
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult Success(string? sessionId, int orderId)
        {
            ViewBag.OrderId = orderId;
            ViewBag.SessionId = sessionId;
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Cancel(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                // Read raw body from middleware (Task 3)
                var json = HttpContext.Items["RawBody"] as string;
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogWarning("Webhook received with no body");
                    return BadRequest("No request body");
                }

                // Read Stripe signature header
                var stripeSignature = Request.Headers["Stripe-Signature"].ToString();
                if (string.IsNullOrEmpty(stripeSignature))
                {
                    _logger.LogWarning("Webhook received with no Stripe-Signature header");
                    return BadRequest("Missing Stripe signature");
                }

                // Read webhook secret from config
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                if (string.IsNullOrEmpty(webhookSecret) || webhookSecret.Contains("REPLACE"))
                {
                    _logger.LogWarning("Stripe webhook secret not configured (placeholder value)");
                    // Graceful handling: log but return OK to prevent Stripe retries during dev
                    return Ok();
                }

                // Verify signature using Stripe.net
                Stripe.Event stripeEvent;
                try
                {
                    stripeEvent = Stripe.EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
                }
                catch (Stripe.StripeException ex)
                {
                    _logger.LogError(ex, "Stripe webhook signature verification failed");
                    return BadRequest("Invalid signature");
                }

                // Handle payment_intent.succeeded event
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
                    if (paymentIntent != null)
                    {
                        _logger.LogInformation("Payment succeeded for PaymentIntent {PaymentIntentId}", paymentIntent.Id);
                        await _orderService.ConfirmPaymentByIntentIdAsync(paymentIntent.Id);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
