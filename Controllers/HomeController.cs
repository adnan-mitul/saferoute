using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using SafeRoute.Models;
using SafeRoute.Data;
using Microsoft.EntityFrameworkCore;

namespace UsersApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalReports = await _context.IncidentReports.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.AreasVerified = await _context.SafetyRatings.CountAsync();
            ViewBag.SOSAlerts = await _context.SosAlerts.CountAsync();

            var recentIncidents = await _context.IncidentReports
                .Include(i => i.Category)
                .OrderByDescending(i => i.ReportDate)
                .Take(5)
                .Select(i => new {
                    i.Id,
                    Category = i.Category != null ? i.Category.Name : "Unknown",
                    i.Description,
                    Location = i.LocationName ?? "Unknown",
                    i.ReportDate,
                    i.Status
                })
                .ToListAsync();
            ViewBag.RecentIncidents = recentIncidents;

            return View();
        }

        public IActionResult Map() => View();

        public async Task<IActionResult> Incidents()
        {
            var incidents = await _context.IncidentReports
                .Include(i => i.Category)
                .OrderByDescending(i => i.ReportDate)
                .ToListAsync();
            return View(incidents);
        }

        public async Task<IActionResult> Analytics()
        {
            ViewBag.TotalReports = await _context.IncidentReports.CountAsync();
            ViewBag.PendingReports = await _context.IncidentReports.CountAsync(r => r.Status == "Pending");
            ViewBag.VerifiedReports = await _context.IncidentReports.CountAsync(r => r.Status == "Verified");
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalSOS = await _context.SosAlerts.CountAsync();
            ViewBag.TotalRatings = await _context.SafetyRatings.CountAsync();

            // Data for Chart.js
            var categoryData = await _context.IncidentReports
                .Include(r => r.Category)
                .GroupBy(r => r.Category.Name)
                .Select(g => new { Label = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.CategoryLabels = System.Text.Json.JsonSerializer.Serialize(categoryData.Select(c => c.Label));
            ViewBag.CategoryCounts = System.Text.Json.JsonSerializer.Serialize(categoryData.Select(c => c.Count));

            // Monthly trend
            var monthlyRaw = await _context.IncidentReports
                .GroupBy(r => new { r.ReportDate.Year, r.ReportDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();
            var monthlyData = monthlyRaw.Select(m => new { Month = $"{m.Year}-{m.Month:D2}", m.Count }).ToList();
            ViewBag.MonthLabels = System.Text.Json.JsonSerializer.Serialize(monthlyData.Select(m => m.Month));
            ViewBag.MonthCounts = System.Text.Json.JsonSerializer.Serialize(monthlyData.Select(m => m.Count));

            return View();
        }

        // GET: /Home/Track/{token}
        public async Task<IActionResult> Track(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var log = await _context.LocationLogs
                .Where(l => l.TrackingToken == id && l.IsActive && l.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            if (log == null)
            {
                ViewBag.Expired = true;
                return View();
            }

            var user = await _context.Users.FindAsync(log.UserId);
            ViewBag.Expired = false;
            ViewBag.Token = id;
            ViewBag.UserName = user?.FullName ?? "User";
            ViewBag.Latitude = log.Latitude;
            ViewBag.Longitude = log.Longitude;
            ViewBag.ExpiresAt = log.ExpiresAt;

            return View();
        }

        public IActionResult SOSGuide() => View();

        [Authorize]
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
