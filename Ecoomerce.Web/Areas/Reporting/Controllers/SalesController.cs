using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecoomerce.Web.Areas.Reporting.Controllers
{
    [Area("Reporting")]
    [Authorize(Roles = "Admin")]
    public class SalesController : Controller
    {
        private readonly ISalesReportService _salesReportService;

        public SalesController(ISalesReportService salesReportService)
        {
            _salesReportService = salesReportService;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);

                var summary = await _salesReportService.GetSalesSummaryAsync(start, end);
                var salesReport = await _salesReportService.GetSalesReportAsync(start, end);
                
                ViewBag.Summary = summary;
                ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                ViewBag.EndDate = end.ToString("yyyy-MM-dd");
                
                return View(salesReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load sales report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.SalesReportDto>());
            }
        }

        public async Task<IActionResult> DailySales(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);
                
                var dailySales = await _salesReportService.GetDailySalesAsync(start, end);
                ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                ViewBag.EndDate = end.ToString("yyyy-MM-dd");
                
                return View(dailySales);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load daily sales report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.DailySalesDto>());
            }
        }

        public async Task<IActionResult> TopProducts(int count = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var topProducts = await _salesReportService.GetTopSellingProductsAsync(count, startDate, endDate);
                ViewBag.Count = count;
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                
                return View(topProducts);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load top products report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.TopSellingProductDto>());
            }
        }

        public async Task<IActionResult> Revenue(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var totalRevenue = await _salesReportService.GetTotalRevenueAsync(startDate, endDate);
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load revenue report: " + ex.Message;
                ViewBag.TotalRevenue = 0m;
                return View();
            }
        }
    }
}
