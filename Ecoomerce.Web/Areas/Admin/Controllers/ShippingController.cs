using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Enums;
using System.Security.Claims;

namespace Ecoomerce.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ShippingController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IActivityLogService _activityLogService;
        private readonly ILogger<ShippingController> _logger;

        public ShippingController(
            IOrderService orderService,
            IEmailSenderService emailSenderService,
            IActivityLogService activityLogService,
            ILogger<ShippingController> logger)
        {
            _orderService = orderService;
            _emailSenderService = emailSenderService;
            _activityLogService = activityLogService;
            _logger = logger;
        }

        // GET: Admin/Shipping
        public async Task<IActionResult> Index(string status = "all", string search = "", int page = 1)
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();

                // Filter by status
                if (!string.IsNullOrEmpty(status) && status != "all")
                {
                    if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                    {
                        orders = orders.Where(o => o.Status == orderStatus);
                    }
                }

                // Search by order ID or user
                if (!string.IsNullOrEmpty(search))
                {
                    orders = orders.Where(o => 
                        o.OrderID.ToString().Contains(search) ||
                        (o.UserID != null && o.UserID.Contains(search, StringComparison.OrdinalIgnoreCase))
                    );
                }

                // Sort by date descending
                orders = orders.OrderByDescending(o => o.OrderDate);

                ViewBag.CurrentStatus = status;
                ViewBag.SearchQuery = search;
                ViewBag.CurrentPage = page;

                return View(orders.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shipping management");
                TempData["Error"] = "Failed to load orders.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // GET: Admin/Shipping/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _orderService.GetOrderDetailsAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order details for order {OrderId}", id);
                TempData["Error"] = "Failed to load order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Shipping/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus, string trackingNumber = "")
        {
            try
            {
                if (!Enum.TryParse<OrderStatus>(newStatus, true, out var status))
                {
                    TempData["Error"] = "Invalid order status.";
                    return RedirectToAction(nameof(Details), new { id = orderId });
                }

                var order = await _orderService.GetOrderDetailsAsync(orderId);
                if (order == null)
                {
                    TempData["Error"] = "Order not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Update order status
                await _orderService.UpdateOrderStatusAsync(orderId, status);

                // Log activity
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _activityLogService.LogActivityAsync(
                    userId,
                    "OrderStatusUpdated",
                    "Order",
                    orderId,
                    $"Order status updated to {status}"
                );

                // If status is shipped, send shipping notification email
                if (status == OrderStatus.shipped && !string.IsNullOrEmpty(trackingNumber))
                {
                    try
                    {
                        await _emailSenderService.SendShippingNotificationEmailAsync(
                            orderId,
                            order.UserEmail ?? "",
                            trackingNumber
                        );

                        await _activityLogService.LogActivityAsync(
                            userId,
                            "ShippingNotificationSent",
                            "Order",
                            orderId,
                            $"Shipping notification sent with tracking: {trackingNumber}"
                        );

                        TempData["Success"] = $"Order status updated to {status} and shipping notification sent!";
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send shipping notification for order {OrderId}", orderId);
                        TempData["Warning"] = $"Order status updated, but failed to send email notification.";
                    }
                }
                else
                {
                    TempData["Success"] = $"Order status updated to {status}.";
                }

                return RedirectToAction(nameof(Details), new { id = orderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", orderId);
                TempData["Error"] = "Failed to update order status.";
                return RedirectToAction(nameof(Details), new { id = orderId });
            }
        }

        // POST: Admin/Shipping/BulkUpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateStatus(int[] orderIds, string newStatus)
        {
            try
            {
                if (orderIds == null || orderIds.Length == 0)
                {
                    TempData["Error"] = "No orders selected.";
                    return RedirectToAction(nameof(Index));
                }

                if (!Enum.TryParse<OrderStatus>(newStatus, true, out var status))
                {
                    TempData["Error"] = "Invalid order status.";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var updatedCount = 0;

                foreach (var orderId in orderIds)
                {
                    try
                    {
                        await _orderService.UpdateOrderStatusAsync(orderId, status);
                        
                        await _activityLogService.LogActivityAsync(
                            userId,
                            "OrderStatusUpdated",
                            "Order",
                            orderId,
                            $"Bulk update: Order status updated to {status}"
                        );

                        updatedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to update order {OrderId} in bulk update", orderId);
                    }
                }

                TempData["Success"] = $"Successfully updated {updatedCount} order(s) to {status}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk status update");
                TempData["Error"] = "Failed to update orders.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
