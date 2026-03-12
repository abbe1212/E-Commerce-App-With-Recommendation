using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecoomerce.Web.Areas.Reporting.Controllers
{
    [Area("Reporting")]
    [Authorize(Roles = "Admin")]
    public class InventoryController : Controller
    {
        private readonly IInventoryReportService _inventoryReportService;

        public InventoryController(IInventoryReportService inventoryReportService)
        {
            _inventoryReportService = inventoryReportService;
        }

        public async Task<IActionResult> Index(int? lowStockThreshold)
        {
            try
            {
                var summary = await _inventoryReportService.GetInventorySummaryAsync();
                var inventoryReport = await _inventoryReportService.GetInventoryReportAsync(lowStockThreshold);
                
                ViewBag.Summary = summary;
                ViewBag.LowStockThreshold = lowStockThreshold ?? 10;
                
                return View(inventoryReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load inventory report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.InventoryReportDto>());
            }
        }

        public async Task<IActionResult> LowStock(int threshold = 10)
        {
            try
            {
                var lowStockProducts = await _inventoryReportService.GetLowStockProductsAsync(threshold);
                ViewBag.Threshold = threshold;
                return View(lowStockProducts);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load low stock report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.InventoryReportDto>());
            }
        }

        public async Task<IActionResult> OutOfStock()
        {
            try
            {
                var outOfStockProducts = await _inventoryReportService.GetOutOfStockProductsAsync();
                return View(outOfStockProducts);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load out of stock report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.InventoryReportDto>());
            }
        }

        public async Task<IActionResult> InventoryLogs(DateTime? startDate, DateTime? endDate, int? productId)
        {
            try
            {
                var logs = await _inventoryReportService.GetInventoryLogsAsync(startDate, endDate, productId);
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.ProductId = productId;
                return View(logs);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load inventory logs: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.InventoryLogReportDto>());
            }
        }
    }
}
