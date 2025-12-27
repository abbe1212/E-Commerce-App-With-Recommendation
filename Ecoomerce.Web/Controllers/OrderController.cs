using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IAppAuthService = Ecommerce.Application.Services.Interfaces.IAuthorizationService;

namespace Ecoomerce.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;
        private readonly IActivityLogService _activityLogService;
        private readonly IAppAuthService _authService;

        public OrderController(
            IOrderService orderService,
            ILogger<OrderController> logger,
            IActivityLogService activityLogService,
            IAppAuthService authService)
        {
            _orderService = orderService;
            _logger = logger;
            _activityLogService = activityLogService;
            _authService = authService;
        }

        // GET: Order - List all user orders
        public async Task<IActionResult> Index(string? status = null, string? sortBy = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var orders = await _orderService.GetUserOrdersAsync(userId);
                
                // Filter by status if provided
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                {
                    orders = orders.Where(o => o.Status == orderStatus);
                }

                // Sort orders
                orders = sortBy?.ToLower() switch
                {
                    "date_asc" => orders.OrderBy(o => o.OrderDate),
                    "total_desc" => orders.OrderByDescending(o => o.TotalAmount),
                    "total_asc" => orders.OrderBy(o => o.TotalAmount),
                    _ => orders.OrderByDescending(o => o.OrderDate) // Default: newest first
                };

                ViewBag.CurrentStatus = status;
                ViewBag.CurrentSort = sortBy;

                return View(orders.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order history");
                TempData["Error"] = "Failed to load your orders. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Order/Details/5 - View single order details
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Check if user can access this order
                var canAccess = await _authService.CanUserAccessOrderAsync(userId, id);
                if (!canAccess)
                {
                    TempData["Error"] = "You don't have permission to view this order.";
                    return RedirectToAction("Index");
                }

                var orderDetails = await _orderService.GetOrderDetailsAsync(id);
                
                if (orderDetails == null)
                {
                    return NotFound();
                }

                return View(orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for order {OrderId}", id);
                TempData["Error"] = "Failed to load order details. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // POST: Order/Cancel/5 - Cancel an order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Please log in to cancel orders." });
                }

                // Check if user can access this order
                var canAccess = await _authService.CanUserAccessOrderAsync(userId, id);
                if (!canAccess)
                {
                    return Json(new { success = false, message = "You don't have permission to cancel this order." });
                }

                // Get order to check status
                var order = await _orderService.GetOrderDetailsAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                // Only allow cancellation if order is pending or processing
                if (order.Status != OrderStatus.pending && order.Status != OrderStatus.processing)
                {
                    return Json(new { success = false, message = $"Cannot cancel order with status '{order.Status}'. Only pending or processing orders can be cancelled." });
                }

                await _orderService.CancelOrderAsync(id);

                // Log activity
                await _activityLogService.LogActivityAsync(
                    userId,
                    "OrderCancelled",
                    "Order",
                    id,
                    $"Order #{id} cancelled by user"
                );

                return Json(new { success = true, message = "Order has been cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return Json(new { success = false, message = "Failed to cancel order. Please try again." });
            }
        }

        // POST: Order/Reorder/5 - Add all items from an order back to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id, [FromServices] ICartService cartService, [FromServices] IProductService productService)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Please log in to reorder." });
                }

                // Check if user can access this order
                var canAccess = await _authService.CanUserAccessOrderAsync(userId, id);
                if (!canAccess)
                {
                    return Json(new { success = false, message = "You don't have permission to reorder this order." });
                }

                var order = await _orderService.GetOrderDetailsAsync(id);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found." });
                }

                int addedCount = 0;
                var unavailableItems = new List<string>();

                foreach (var item in order.Items)
                {
                    // Check if product is still available
                    var product = await productService.GetProductByIdAsync(item.ProductID);
                    if (product != null && product.IsAvailable && product.StockQuantity >= item.Quantity)
                    {
                        await cartService.AddItemToCartAsync(userId, item.ProductID, item.Quantity);
                        addedCount++;
                    }
                    else
                    {
                        unavailableItems.Add(item.ProductName);
                    }
                }

                // Log activity
                await _activityLogService.LogActivityAsync(
                    userId,
                    "OrderReordered",
                    "Order",
                    id,
                    $"Reordered {addedCount} items from order #{id}"
                );

                if (unavailableItems.Any())
                {
                    return Json(new { 
                        success = true, 
                        message = $"Added {addedCount} items to cart. Some items were unavailable: {string.Join(", ", unavailableItems)}",
                        redirect = Url.Action("Index", "Cart")
                    });
                }

                return Json(new { 
                    success = true, 
                    message = $"All {addedCount} items added to cart!",
                    redirect = Url.Action("Index", "Cart")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering from order {OrderId}", id);
                return Json(new { success = false, message = "Failed to add items to cart. Please try again." });
            }
        }

        // GET: Order/Track/5 - Track order status
        public async Task<IActionResult> Track(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account");
                }

                var canAccess = await _authService.CanUserAccessOrderAsync(userId, id);
                if (!canAccess)
                {
                    TempData["Error"] = "You don't have permission to track this order.";
                    return RedirectToAction("Index");
                }

                var orderDetails = await _orderService.GetOrderDetailsAsync(id);
                if (orderDetails == null)
                {
                    return NotFound();
                }

                return View(orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking order {OrderId}", id);
                TempData["Error"] = "Failed to load tracking information. Please try again.";
                return RedirectToAction("Index");
            }
        }
    }
}
