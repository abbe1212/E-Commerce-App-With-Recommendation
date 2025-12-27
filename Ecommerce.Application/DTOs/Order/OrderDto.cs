using Ecommerce.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Order
{
    public class OrderDto
    {
        public int OrderID { get; set; }
        public string UserID { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string ShippingAddress { get; set; }
        public string? ShippingMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemsCount { get; set; }
        public string TrackingNumber { get; set; } 
    }
}
