using Ecommerce.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Payment
{
    public class PaymentResultDto
    {

        public bool IsSuccess { get; set; }
        public int? PaymentId { get; set; }
        public PaymentStatus Status { get; set; } // e.g., Paid, Failed, Pending
        public string? TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Message { get; set; } = string.Empty; // Success or error message
        public string RedirectUrl { get; set; } = string.Empty;
        public string? ClientSecret { get; set; } // For Stripe payment confirmation
    }
}
