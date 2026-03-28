using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeRoute.Models
{
    public class LocationLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public Users? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string TrackingToken { get; set; } = string.Empty; // Unique token for sharing

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } // Auto-expire after 1 hour

        public bool IsActive { get; set; } = true;
    }
}

