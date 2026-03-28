using System.ComponentModel.DataAnnotations;

namespace SafeRoute.Models
{
    public class EmergencyService
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "Police"; // Police, Hospital

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        [MaxLength(20)]
        public string ContactNumber { get; set; } = string.Empty;
    }
}

