using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafeRoute.Models;

namespace SafeRoute.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public DbSet<IncidentCategory> IncidentCategories { get; set; }
        public DbSet<IncidentReport> IncidentReports { get; set; }
        public DbSet<SafetyRating> SafetyRatings { get; set; }
        public DbSet<EmergencyService> EmergencyServices { get; set; }
        public DbSet<TrustedContact> TrustedContacts { get; set; }
        public DbSet<IncidentVote> IncidentVotes { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<LocationLog> LocationLogs { get; set; }
        public DbSet<SosAlert> SosAlerts { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique constraint: one vote per user per incident
            builder.Entity<IncidentVote>()
                .HasIndex(v => new { v.UserId, v.IncidentId })
                .IsUnique();
        }
    }
}
