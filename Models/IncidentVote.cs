using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SafeRoute.Models
{
    public class IncidentVote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public Users? User { get; set; }

        [Required]
        public int IncidentId { get; set; }
        [ForeignKey("IncidentId")]
        public IncidentReport? Incident { get; set; }

        public bool IsConfirm { get; set; } // true = Confirm, false = Fake

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    }
}

