using System.ComponentModel.DataAnnotations;

namespace SafeRoute.Models
{
    public class IncidentCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int SeverityWeight { get; set; } = 1;
    }
}

