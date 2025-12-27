using Ecommerce.Core.Entities;
using Ecommerce.Core.Enums;
using Ecommerce.Core.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Infrastructure.Repositories
{
    public class PaymentRepository : Repository<Payment>, IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }


        // Done
        public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                 .FirstOrDefaultAsync(p => p.OrderID == orderId);
        }

        // Get payment by transaction ID
        public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.Payments
                 .FirstOrDefaultAsync(p => p.TransactionID == transactionId);
        }

        // Done
        public async Task<IEnumerable<Payment>> GetPaymentsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Payment>> GetPaymentsByMethodAsync(PaymentMethod method)
        {
            return await _context.Payments
                .Join(_context.Orders, p => p.OrderID, o => o.OrderID, (p, o) => new { Payment = p, Order = o })
                .Where(x => x.Order.PaymentMethod == method)
                .Select(x => x.Payment)
                .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
        {
            return await _context.Payments
                            .Where(p => p.PaymentStatus == status)
                            .ToListAsync();
        }

        // Done
        public async Task<IEnumerable<Payment>> GetRecentPaymentsAsync(int count)
        {
            return await _context.Payments
                .OrderByDescending(p => p.PaymentDate)
                .Take(count)
                .ToListAsync();
        }

        //Done
        public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Payments.Where(p => p.PaymentStatus == PaymentStatus.Paid);

            if (startDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= endDate.Value);
            }

            // Sum the Amount property
            return await query.SumAsync(p => (decimal?)p.Amount) ?? 0m;
        }
    }
}
