using Ecommerce.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.ViewModels
{
    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new();
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Phone { get; set; } = string.Empty;
        
        [Required]
        public string ShippingAddress { get; set; } = string.Empty;
        
        public string? BillingAddress { get; set; }
        
        [Required]
        public string City { get; set; } = string.Empty;
        
        [Required]
        public string State { get; set; } = string.Empty;
        
        [Required]
        public string ZipCode { get; set; } = string.Empty;
        
        [Required]
        public string Country { get; set; } = string.Empty;
        
        public string PaymentMethod { get; set; } = string.Empty;
        
        public string? CardNumber { get; set; }
        
        public string? CardExpiry { get; set; }
        
        public string? CardCVV { get; set; }
        
        public string? CardLastFour { get; set; }
        
        public bool UseShippingAsBilling { get; set; } = true;
        
        public bool AgreeToTerms { get; set; } = false;
        
        // Shipping Method Selection
        public string ShippingMethod { get; set; } = "standard";
        
        // Order Notes / Special Instructions
        [MaxLength(500)]
        public string? OrderNotes { get; set; }
        
        // Promo Code
        public string? PromoCode { get; set; }
        
        // Applied promo code discount
        public decimal PromoDiscount { get; set; } = 0;
        
        // Message about promo code status
        public string? PromoCodeMessage { get; set; }
        
        // Estimated delivery date based on shipping method
        public DateTime? EstimatedDeliveryDate { get; set; }
    }
}
