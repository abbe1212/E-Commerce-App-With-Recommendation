using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Application.ViewModels;
using Ecommerce.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecoomerce.Web.Areas.Profile.Controllers
{
    [Area("Profile")] 
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IWishlistService _wishlistService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(
            IOrderService orderService,
            IWishlistService wishlistService,
            UserManager<ApplicationUser> userManager)
        {
            _orderService = orderService;
            _wishlistService = wishlistService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Fetch user data
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Fetch orders
            var orders = await _orderService.GetUserOrdersAsync(userId);
            var ordersList = orders.ToList();
            var recentOrders = ordersList.OrderByDescending(o => o.OrderDate).Take(5).ToList();

            // Fetch wishlist
            var wishlist = await _wishlistService.GetWishlistAsync(userId);
            var wishlistCount = wishlist?.Items?.Count ?? 0;

            // Calculate profile completeness
            int filledFields = 0;
            int totalFields = 4; // FirstName, LastName, PhoneNumber, ImageUrl
            
            if (!string.IsNullOrEmpty(user.FirstName)) filledFields++;
            if (!string.IsNullOrEmpty(user.LastName)) filledFields++;
            if (!string.IsNullOrEmpty(user.PhoneNumber)) filledFields++;
            if (!string.IsNullOrEmpty(user.ImageUrl)) filledFields++;
            
            int profileCompleteness = (int)((filledFields / (double)totalFields) * 100);

            // Build ViewModel
            var viewModel = new ProfileDashboardViewModel
            {
                FirstName = user.FirstName,
                TotalOrders = ordersList.Count,
                WishlistItems = wishlistCount,
                ProfileCompleteness = profileCompleteness,
                RecentOrders = recentOrders
            };

            return View(viewModel);
        }
    }
}
