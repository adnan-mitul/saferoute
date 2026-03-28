using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SafeRoute.Models
{
    public class Users : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber2 { get; set; } // Secondary phone

        [MaxLength(200)]
        public string? Address { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
