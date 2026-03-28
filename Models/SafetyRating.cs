using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeRoute.Models
{
    public class SafetyRating
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public Users? User { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Range(1, 5)]
        public int LightingScore { get; set; }

        [Range(1, 5)]
        public int CrowdScore { get; set; }

        [Range(1, 5)]
        public int PoliceScore { get; set; }

        [Range(1, 5)]
        public int OverallScore { get; set; }

        public DateTime RatingDate { get; set; } = DateTime.UtcNow;
    }
}

