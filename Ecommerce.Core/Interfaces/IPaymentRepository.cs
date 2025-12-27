using Ecommerce.Core.Entities;
using Ecommerce.Core.Enums;

namespace Ecommerce.Core.Interfaces
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        /// Gets a payment by its associated Order ID.
        Task<Payment?> GetPaymentByOrderIdAsync(int orderId);

        /// Gets a payment by its transaction ID.
        Task<Payment?> GetByTransactionIdAsync(string transactionId);

        /// Gets all payments with a specific status.
        Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);

        /// Gets all payments processed using a specific payment method.
        Task<IEnumerable<Payment>> GetPaymentsByMethodAsync(PaymentMethod method); // Assuming PaymentMethod is relevant or stored

        /// Gets payments within a specific date range.
        Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// Gets the N most recent payments.
        Task<IEnumerable<Payment>> GetRecentPaymentsAsync(int count);

        /// Calculates the total revenue from successful payments within an optional date range.
        Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}
