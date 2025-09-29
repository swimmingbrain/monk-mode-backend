using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using monk_mode_backend.Domain;

namespace monk_mode_backend.Infrastructure {
    public class MonkModeDbContext : IdentityDbContext<ApplicationUser> {
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<UserTask> Tasks { get; set; }
        public DbSet<TimeBlock> TimeBlocks { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateBlock> TemplateBlocks { get; set; }
        public DbSet<DailyStatistics> DailyStatistics { get; set; }

        public MonkModeDbContext(DbContextOptions<MonkModeDbContext> options) : base(options) {
        }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            // Configure Template and TemplateBlock relationships
            builder.Entity<Template>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TemplateBlock>()
                .HasOne(tb => tb.Template)
                .WithMany(t => t.TemplateBlocks)
                .HasForeignKey(tb => tb.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure DailyStatistics relationship
            builder.Entity<DailyStatistics>()
                .HasOne(ds => ds.User)
                .WithMany()
                .HasForeignKey(ds => ds.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
