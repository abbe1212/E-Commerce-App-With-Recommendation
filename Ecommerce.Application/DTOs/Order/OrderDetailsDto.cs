using Ecommerce.Application.DTOs.Payment;
using Ecommerce.Application.DTOs.Shipping;
using Ecommerce.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Order
{
    public class OrderDetailsDto
    {
        public int OrderID { get; set; }
        public string UserID { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string ShippingAddress { get; set; }= string.Empty;
        public string? ShippingMethod { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public string? OrderNotes { get; set; }
        public decimal TotalAmount { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
        public PaymentDto? Payment { get; set; }
        public ShippingDto? Shipping { get; set; }
    }
}
