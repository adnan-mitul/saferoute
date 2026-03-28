using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRoute.Data;
using SafeRoute.Models;

namespace UsersApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public AdminController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalReports = await _context.IncidentReports.CountAsync();
            ViewBag.PendingReports = await _context.IncidentReports.CountAsync(r => r.Status == "Pending");
            ViewBag.VerifiedReports = await _context.IncidentReports.CountAsync(r => r.Status == "Verified");
            ViewBag.RejectedReports = await _context.IncidentReports.CountAsync(r => r.Status == "Rejected");
            ViewBag.TotalSOS = await _context.SosAlerts.CountAsync();
            ViewBag.EmergencyServices = await _context.EmergencyServices.CountAsync();
            ViewBag.RecentReports = await _context.IncidentReports
                .Include(r => r.Category)
                .OrderByDescending(r => r.ReportDate)
                .Take(10)
                .ToListAsync();
            return View();
        }

        // GET: /Admin/ManageReports
        public async Task<IActionResult> ManageReports(string? status)
        {
            var query = _context.IncidentReports
                .Include(r => r.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var reports = await query.OrderByDescending(r => r.ReportDate).ToListAsync();
            ViewBag.CurrentFilter = status;
            return View(reports);
        }

        // POST: /Admin/UpdateReportStatus
        [HttpPost]
        public async Task<IActionResult> UpdateReportStatus(int id, string status)
        {
            var report = await _context.IncidentReports.FindAsync(id);
            if (report != null)
            {
                report.Status = status;
                await _context.SaveChangesAsync();

                // Create notification for the reporter
                if (!string.IsNullOrEmpty(report.UserId))
                {
                    var notification = new Notification
                    {
                        UserId = report.UserId,
                        Title = $"Report {status}",
                        Message = $"Your incident report #{report.Id} has been {status.ToLower()} by admin.",
                        Type = "ReportUpdate",
                        Link = "/Home/Incidents"
                    };
                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("ManageReports");
        }

        // GET: /Admin/ManageUsers
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: /Admin/ManageEmergency
        public async Task<IActionResult> ManageEmergency()
        {
            var services = await _context.EmergencyServices.ToListAsync();
            return View(services);
        }

        // POST: /Admin/AddEmergency
        [HttpPost]
        public async Task<IActionResult> AddEmergency(EmergencyService service)
        {
            if (!string.IsNullOrEmpty(service.Name))
            {
                _context.EmergencyServices.Add(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageEmergency");
        }

        // POST: /Admin/DeleteEmergency
        [HttpPost]
        public async Task<IActionResult> DeleteEmergency(int id)
        {
            var service = await _context.EmergencyServices.FindAsync(id);
            if (service != null)
            {
                _context.EmergencyServices.Remove(service);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageEmergency");
        }
    }
}

