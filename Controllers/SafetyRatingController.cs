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
    public class SafetyRatingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SafetyRatingController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/SafetyRating
        [HttpGet]
        public async Task<IActionResult> GetRatings()
        {
            var ratings = await _context.SafetyRatings
                .GroupBy(r => new { Lat = Math.Round(r.Latitude, 3), Lng = Math.Round(r.Longitude, 3) })
                .Select(g => new
                {
                    latitude = g.Key.Lat,
                    longitude = g.Key.Lng,
                    avgLighting = Math.Round(g.Average(r => r.LightingScore), 1),
                    avgCrowd = Math.Round(g.Average(r => r.CrowdScore), 1),
                    avgPolice = Math.Round(g.Average(r => r.PoliceScore), 1),
                    avgOverall = Math.Round(g.Average(r => r.OverallScore), 1),
                    totalRatings = g.Count()
                })
                .ToListAsync();
            return Ok(ratings);
        }

        // POST: /api/SafetyRating
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddRating([FromBody] SafetyRatingDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var rating = new SafetyRating
            {
                UserId = userId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                LightingScore = dto.LightingScore,
                CrowdScore = dto.CrowdScore,
                PoliceScore = dto.PoliceScore,
                OverallScore = dto.OverallScore
            };

            _context.SafetyRatings.Add(rating);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Rating submitted successfully" });
        }
    }

    public class SafetyRatingDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int LightingScore { get; set; }
        public int CrowdScore { get; set; }
        public int PoliceScore { get; set; }
        public int OverallScore { get; set; }
    }
}

