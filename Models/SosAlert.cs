using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeRoute.Models
{
    public class SosAlert
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public Users? User { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [MaxLength(200)]
        public string? LocationName { get; set; }

        [MaxLength(50)]
        public string TriggerMethod { get; set; } = "Button"; // Button, Shake, Keyboard

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Resolved, Cancelled

        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}

