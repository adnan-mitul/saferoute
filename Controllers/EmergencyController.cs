using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SafeRoute.Data;

namespace UsersApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmergencyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmergencyController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/Emergency
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var services = await _context.EmergencyServices
                .Select(e => new
                {
                    e.Id,
                    e.Type,
                    e.Name,
                    e.Latitude,
                    e.Longitude,
                    e.ContactNumber
                })
                .ToListAsync();
            return Ok(services);
        }
    }
}

