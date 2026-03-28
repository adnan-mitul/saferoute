using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRoute.Data;
using SafeRoute.Models;
using System.Security.Claims;

namespace UsersApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SosController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /api/Sos/trigger
        [HttpPost("trigger")]
        [Authorize]
        public async Task<IActionResult> TriggerSos([FromBody] SosTriggerDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var alert = new SosAlert
            {
                UserId = userId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                LocationName = dto.LocationName,
                TriggerMethod = dto.TriggerMethod ?? "Button"
            };

            _context.SosAlerts.Add(alert);

            // Create location log for tracking
            var token = Guid.NewGuid().ToString("N")[..12];
            var locationLog = new LocationLog
            {
                UserId = userId,
                TrackingToken = token,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                IsActive = true
            };
            _context.LocationLogs.Add(locationLog);
            await _context.SaveChangesAsync();

            // Get trusted contacts
            var contacts = await _context.TrustedContacts
                .Where(c => c.UserId == userId)
                .Select(c => new { c.Name, c.Phone })
                .ToListAsync();

            var user = await _context.Users.FindAsync(userId);

            return Ok(new
            {
                alertId = alert.Id,
                trackingToken = token,
                trackingUrl = $"/Home/Track/{token}",
                contacts,
                userName = user?.FullName ?? "SafeRoute User",
                message = "SOS triggered successfully"
            });
        }

        // POST: /api/Sos/resolve/{id}
        [HttpPost("resolve/{id}")]
        [Authorize]
        public async Task<IActionResult> ResolveSos(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var alert = await _context.SosAlerts
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
            if (alert != null)
            {
                alert.Status = "Resolved";
                alert.ResolvedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        // POST: /api/Sos/location
        [HttpPost("location")]
        [Authorize]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var log = await _context.LocationLogs
                .Where(l => l.UserId == userId && l.TrackingToken == dto.Token && l.IsActive)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            if (log != null && log.ExpiresAt > DateTime.UtcNow)
            {
                var newLog = new LocationLog
                {
                    UserId = userId,
                    TrackingToken = dto.Token,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    ExpiresAt = log.ExpiresAt,
                    IsActive = true
                };
                _context.LocationLogs.Add(newLog);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        // GET: /api/Sos/track/{token}
        [HttpGet("track/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTracking(string token)
        {
            var log = await _context.LocationLogs
                .Where(l => l.TrackingToken == token && l.IsActive && l.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            if (log == null)
                return NotFound(new { message = "Tracking link expired or not found" });

            var user = await _context.Users.FindAsync(log.UserId);

            return Ok(new
            {
                latitude = log.Latitude,
                longitude = log.Longitude,
                timestamp = log.Timestamp,
                userName = user?.FullName ?? "User",
                expiresAt = log.ExpiresAt
            });
        }
    }

    public class SosTriggerDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? LocationName { get; set; }
        public string? TriggerMethod { get; set; }
    }

    public class LocationUpdateDto
    {
        public string Token { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

