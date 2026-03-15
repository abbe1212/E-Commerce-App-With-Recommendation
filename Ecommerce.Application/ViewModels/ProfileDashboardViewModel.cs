using Ecommerce.Application.DTOs.Order;

namespace Ecommerce.Application.ViewModels
{
    public class ProfileDashboardViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int WishlistItems { get; set; }
        public int ProfileCompleteness { get; set; }
        public List<OrderDto> RecentOrders { get; set; } = new();
    }
}
