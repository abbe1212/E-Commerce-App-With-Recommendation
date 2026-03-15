using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ecommerce.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderManagementController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderManagementController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // Displays a list of all orders with filtering, search, and pagination
        public async Task<IActionResult> Index(string statusFilter, string searchTerm, int page = 1)
        {
            const int pageSize = 20;
            
            var allOrders = await _orderService.GetAllOrdersAsync();
            var ordersList = allOrders.ToList();

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<OrderStatus>(statusFilter, true, out var status))
            {
                ordersList = ordersList.Where(o => o.Status == status).ToList();
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                ordersList = ordersList.Where(o => 
                    o.OrderID.ToString().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    o.UserID.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            // Calculate pagination
            var totalPages = (int)Math.Ceiling(ordersList.Count / (double)pageSize);
            var paginatedOrders = ordersList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass data to view via ViewBag instead of ManageOrdersViewModel
            ViewBag.Orders = paginatedOrders;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View();
        }

        // Updates the status of a single order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, OrderStatus newStatus)
        {
            try
            {
                await _orderService.UpdateOrderStatusAsync(orderId, newStatus);
                TempData["SuccessMessage"] = $"Order #{orderId} status updated to {newStatus}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to update order #{orderId}: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // Updates the status of multiple orders in bulk
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateStatus(int[] orderIds, OrderStatus newStatus)
        {
            if (orderIds == null || orderIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No orders selected.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                foreach (var id in orderIds)
                {
                    await _orderService.UpdateOrderStatusAsync(id, newStatus);
                }
                TempData["SuccessMessage"] = $"{orderIds.Length} order(s) updated to {newStatus}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to update orders: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Displays the details of a single order
        public async Task<IActionResult> Details(int orderId)
        {
            var orderDetails = await _orderService.GetOrderDetailsAsync(orderId);
            if (orderDetails == null)
            {
                return NotFound();
            }
            return View(orderDetails);
        }
    }
}