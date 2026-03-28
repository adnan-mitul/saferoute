using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeRoute.Data;
using SafeRoute.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SafeRoute.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IncidentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public IncidentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetIncidents()
        {
            var incidents = await _context.IncidentReports
                .Include(i => i.Category)
                .Select(i => new {
                    i.Id,
                    i.Latitude,
                    i.Longitude,
                    i.Description,
                    Category = i.Category.Name,
                    i.Status,
                    i.ReportDate,
                    i.LocationName,
                    ConfirmCount = _context.IncidentVotes.Count(v => v.IncidentId == i.Id && v.IsConfirm),
                    FakeCount = _context.IncidentVotes.Count(v => v.IncidentId == i.Id && !v.IsConfirm)
                })
                .ToListAsync();

            return Ok(incidents);
        }

        public class IncidentDto
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int CategoryId { get; set; }
            public string Description { get; set; } = "";
            public bool IsAnonymous { get; set; }
            public string? LocationName { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> ReportIncident([FromBody] IncidentDto dto)
        {
            if (dto.CategoryId <= 0)
                return BadRequest(new { success = false, message = "Category is required." });
            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { success = false, message = "Description is required." });
            if (dto.Latitude == 0 && dto.Longitude == 0)
                return BadRequest(new { success = false, message = "Location is required." });

            var categoryExists = await _context.IncidentCategories.AnyAsync(c => c.Id == dto.CategoryId);
            if (!categoryExists)
                return BadRequest(new { success = false, message = "Invalid category." });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var report = new IncidentReport
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                CategoryId = dto.CategoryId,
                Description = dto.Description,
                IsAnonymous = dto.IsAnonymous,
                UserId = userId,
                LocationName = string.IsNullOrWhiteSpace(dto.LocationName) ? null : dto.LocationName.Trim(),
                ReportDate = DateTime.UtcNow,
                Status = "Pending"
            };

            _context.IncidentReports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Report submitted successfully!", id = report.Id });
        }

        // POST: /api/Incident/vote
        [HttpPost("vote")]
        [Authorize]
        public async Task<IActionResult> VoteIncident([FromBody] VoteDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var existing = await _context.IncidentVotes
                .FirstOrDefaultAsync(v => v.UserId == userId && v.IncidentId == dto.IncidentId);

            if (existing != null)
            {
                existing.IsConfirm = dto.IsConfirm;
                existing.VotedAt = DateTime.UtcNow;
            }
            else
            {
                _context.IncidentVotes.Add(new IncidentVote
                {
                    UserId = userId,
                    IncidentId = dto.IncidentId,
                    IsConfirm = dto.IsConfirm
                });
            }

            await _context.SaveChangesAsync();

            var confirmCount = await _context.IncidentVotes.CountAsync(v => v.IncidentId == dto.IncidentId && v.IsConfirm);
            var fakeCount = await _context.IncidentVotes.CountAsync(v => v.IncidentId == dto.IncidentId && !v.IsConfirm);

            return Ok(new { confirmCount, fakeCount });
        }
    }

    public class VoteDto
    {
        public int IncidentId { get; set; }
        public bool IsConfirm { get; set; }
    }
}

