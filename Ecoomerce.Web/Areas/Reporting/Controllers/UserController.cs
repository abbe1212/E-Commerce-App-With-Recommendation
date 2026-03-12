using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecoomerce.Web.Areas.Reporting.Controllers
{
    [Area("Reporting")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserReportService _userReportService;

        public UserController(IUserReportService userReportService)
        {
            _userReportService = userReportService;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var summary = await _userReportService.GetUserSummaryAsync();
                var userReport = await _userReportService.GetUserReportAsync(startDate, endDate);
                
                ViewBag.Summary = summary;
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                
                return View(userReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load user report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.UserReportDto>());
            }
        }

        public async Task<IActionResult> Activity()
        {
            try
            {
                var activityReport = await _userReportService.GetUserActivityReportAsync();
                return View(activityReport);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load user activity report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.UserActivityDto>());
            }
        }

        public async Task<IActionResult> RegistrationTrend(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);
                
                var registrationTrend = await _userReportService.GetRegistrationTrendAsync(start, end);
                ViewBag.StartDate = start.ToString("yyyy-MM-dd");
                ViewBag.EndDate = end.ToString("yyyy-MM-dd");
                
                return View(registrationTrend);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load registration trend report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.UserRegistrationTrendDto>());
            }
        }

        public async Task<IActionResult> TopCustomers(int count = 10)
        {
            try
            {
                var topCustomers = await _userReportService.GetTopCustomersAsync(count);
                ViewBag.Count = count;
                
                return View(topCustomers);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Failed to load top customers report: " + ex.Message;
                return View(new List<Ecommerce.Application.DTOs.Reporting.UserReportDto>());
            }
        }
    }
}
