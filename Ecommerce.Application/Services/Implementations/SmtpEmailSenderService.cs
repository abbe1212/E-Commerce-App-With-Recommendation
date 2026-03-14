using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Interfaces;
using System.Text;

namespace Ecommerce.Infrastructure.Services
{
    public class SmtpEmailSenderService : IEmailSenderService
    {
        private readonly IConfiguration _configuration;
        private readonly IOrderRepository _orderRepository;

        public SmtpEmailSenderService(IConfiguration configuration, IOrderRepository orderRepository)
        {
            _configuration = configuration;
            _orderRepository = orderRepository;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"];
            var port = int.Parse(smtpSection["Port"]);
            var enableSsl = bool.Parse(smtpSection["EnableSsl"]);
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                Timeout = 5000
            };
            var mail = new MailMessage(username, toEmail, subject, htmlMessage)
            {
                IsBodyHtml = true
            };
            await client.SendMailAsync(mail);
        }

        public async Task SendOrderConfirmationEmailAsync(int orderId, string userEmail)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null) return;

            var subject = $"Order Confirmation - Order #{orderId}";
            var htmlMessage = GenerateOrderConfirmationEmail(order);
            
            await SendEmailAsync(userEmail, subject, htmlMessage);
        }

        public async Task SendShippingNotificationEmailAsync(int orderId, string userEmail, string trackingNumber)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null) return;

            var subject = $"Your Order Has Shipped - Order #{orderId}";
            var htmlMessage = GenerateShippingNotificationEmail(order, trackingNumber);
            
            await SendEmailAsync(userEmail, subject, htmlMessage);
        }

        public async Task SendPaymentReceiptEmailAsync(int orderId, string userEmail, string transactionId)
        {
            var order = await _orderRepository.GetOrderWithDetailsAsync(orderId);
            if (order == null) return;

            var subject = $"Payment Receipt - Order #{orderId}";
            var htmlMessage = GeneratePaymentReceiptEmail(order, transactionId);
            
            await SendEmailAsync(userEmail, subject, htmlMessage);
        }

        private string GenerateOrderConfirmationEmail(Core.Entities.Order order)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.AppendLine(".header { background-color: #0d6efd; color: white; padding: 20px; text-align: center; }");
            sb.AppendLine(".content { padding: 20px; background-color: #f8f9fa; }");
            sb.AppendLine(".order-details { background-color: white; padding: 15px; margin: 15px 0; border-radius: 5px; }");
            sb.AppendLine(".item { border-bottom: 1px solid #dee2e6; padding: 10px 0; }");
            sb.AppendLine(".total { font-size: 1.2em; font-weight: bold; margin-top: 15px; }");
            sb.AppendLine(".footer { text-align: center; padding: 20px; color: #6c757d; font-size: 0.9em; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>Order Confirmation</h1>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<p>Thank you for your order! Your order has been received and is being processed.</p>");
            sb.AppendLine("<div class='order-details'>");
            sb.AppendLine($"<h2>Order #{order.OrderID}</h2>");
            sb.AppendLine($"<p><strong>Order Date:</strong> {order.OrderDate:MMMM dd, yyyy}</p>");
            sb.AppendLine($"<p><strong>Shipping Method:</strong> {order.ShippingMethod}</p>");
            if (order.EstimatedDeliveryDate.HasValue)
            {
                sb.AppendLine($"<p><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryDate.Value:MMMM dd, yyyy}</p>");
            }
            sb.AppendLine($"<p><strong>Shipping Address:</strong><br>{order.ShippingAddress}</p>");
            if (!string.IsNullOrEmpty(order.OrderNotes))
            {
                sb.AppendLine($"<p><strong>Order Notes:</strong> {order.OrderNotes}</p>");
            }
            sb.AppendLine("<h3>Order Items:</h3>");
            foreach (var item in order.OrderItems)
            {
                sb.AppendLine("<div class='item'>");
                sb.AppendLine($"<p><strong>{item.Product?.Name ?? "Product"}</strong></p>");
                sb.AppendLine($"<p>Quantity: {item.Quantity} × {item.UnitPrice:C} = {(item.Quantity * item.UnitPrice):C}</p>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine($"<p class='total'>Total: {order.TotalAmount:C}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>Thank you for shopping with us!</p>");
            sb.AppendLine("<p>If you have any questions, please contact our customer support.</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private string GenerateShippingNotificationEmail(Core.Entities.Order order, string trackingNumber)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.AppendLine(".header { background-color: #198754; color: white; padding: 20px; text-align: center; }");
            sb.AppendLine(".content { padding: 20px; background-color: #f8f9fa; }");
            sb.AppendLine(".tracking-box { background-color: white; padding: 20px; margin: 15px 0; border-radius: 5px; text-align: center; }");
            sb.AppendLine(".tracking-number { font-size: 1.5em; font-weight: bold; color: #0d6efd; padding: 10px; background-color: #e7f3ff; border-radius: 5px; }");
            sb.AppendLine(".footer { text-align: center; padding: 20px; color: #6c757d; font-size: 0.9em; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>Your Order Has Shipped!</h1>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<p>Great news! Your order #{order.OrderID} has been shipped and is on its way to you.</p>");
            sb.AppendLine("<div class='tracking-box'>");
            sb.AppendLine("<h3>Tracking Number</h3>");
            sb.AppendLine($"<div class='tracking-number'>{trackingNumber}</div>");
            if (order.EstimatedDeliveryDate.HasValue)
            {
                sb.AppendLine($"<p style='margin-top: 15px;'><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryDate.Value:MMMM dd, yyyy}</p>");
            }
            sb.AppendLine("</div>");
            sb.AppendLine($"<p><strong>Shipping Address:</strong><br>{order.ShippingAddress}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>Thank you for your order!</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        private string GeneratePaymentReceiptEmail(Core.Entities.Order order, string transactionId)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
            sb.AppendLine(".header { background-color: #0d6efd; color: white; padding: 20px; text-align: center; }");
            sb.AppendLine(".content { padding: 20px; background-color: #f8f9fa; }");
            sb.AppendLine(".receipt-box { background-color: white; padding: 15px; margin: 15px 0; border-radius: 5px; }");
            sb.AppendLine(".footer { text-align: center; padding: 20px; color: #6c757d; font-size: 0.9em; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>Payment Receipt</h1>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<p>Your payment has been successfully processed.</p>");
            sb.AppendLine("<div class='receipt-box'>");
            sb.AppendLine($"<h2>Order #{order.OrderID}</h2>");
            sb.AppendLine($"<p><strong>Transaction ID:</strong> {transactionId}</p>");
            sb.AppendLine($"<p><strong>Payment Date:</strong> {DateTime.UtcNow:MMMM dd, yyyy}</p>");
            sb.AppendLine($"<p><strong>Payment Method:</strong> {order.PaymentMethod}</p>");
            sb.AppendLine($"<p><strong>Amount Paid:</strong> {order.TotalAmount:C}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>Thank you for your payment!</p>");
            sb.AppendLine("<p>This is your official receipt for this transaction.</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }
    }
}
