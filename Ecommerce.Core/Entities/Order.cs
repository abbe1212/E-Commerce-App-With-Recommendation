using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Ecommerce.Core.Enums;

namespace Ecommerce.Core.Entities
{
    public class Order
    {
        public int OrderID { get; set; }

        [Required]
        public string UserID { get; set; }
        public virtual ApplicationUser User { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public OrderStatus Status { get; set; } = OrderStatus.pending;

        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public string ShippingAddress { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Shipping method selection
        [MaxLength(50)]
        public string? ShippingMethod { get; set; }

        // Estimated delivery date based on shipping method
        public DateTime? EstimatedDeliveryDate { get; set; }

        // Special instructions from customer
        [MaxLength(500)]
        public string? OrderNotes { get; set; }

        // Stripe Payment Intent ID for Stripe payment tracking
        [MaxLength(255)]
        public string? PaymentIntentId { get; set; }

        public virtual Payment Payment { get; set; }
        public virtual Shipping Shipping { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<OrderPromoCode> OrderPromoCodes { get; set; } = new List<OrderPromoCode>();

        //public static implicit operator Order(Order v)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
