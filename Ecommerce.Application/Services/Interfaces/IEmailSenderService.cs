using System.Threading.Tasks;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface IEmailSenderService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendOrderConfirmationEmailAsync(int orderId, string userEmail);
        Task SendShippingNotificationEmailAsync(int orderId, string userEmail, string trackingNumber);
        Task SendPaymentReceiptEmailAsync(int orderId, string userEmail, string transactionId);
    }
}
