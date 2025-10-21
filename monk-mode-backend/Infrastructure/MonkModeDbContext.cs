using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using monk_mode_backend.Domain;

namespace monk_mode_backend.Infrastructure
{
    public class MonkModeDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Friendship> Friendships { get; set; }
        public DbSet<UserTask> Tasks { get; set; }
        public DbSet<TimeBlock> TimeBlocks { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateBlock> TemplateBlocks { get; set; }
        public DbSet<DailyStatistics> DailyStatistics { get; set; }

        public MonkModeDbContext(DbContextOptions<MonkModeDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ---------------------------
            // Beziehungen (bestehend + Hardening)
            // ---------------------------

            // Template → User (Owner); Löschen des Users löscht Templates (ok)
            builder.Entity<Template>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // TemplateBlock → Template; beim Löschen des Templates fallen Blöcke mit (ok)
            builder.Entity<TemplateBlock>()
                .HasOne(tb => tb.Template)
                .WithMany(t => t.TemplateBlocks)
                .HasForeignKey(tb => tb.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // DailyStatistics → User; beim Löschen des Users fallen Statistiken mit (ok)
            builder.Entity<DailyStatistics>()
                .HasOne(ds => ds.User)
                .WithMany()
                .HasForeignKey(ds => ds.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // === DailyStatistics: genau eine Zeile pro (User, Date)
            builder.Entity<DailyStatistics>()
                .HasIndex(x => new { x.UserId, x.Date })
                .IsUnique();

            // === Friendship: keine Duplikate derselben Richtung; restriktives Delete
            builder.Entity<Friendship>()
                .HasIndex(x => new { x.UserId, x.FriendId })
                .IsUnique();

            builder.Entity<Friendship>()
                .Property(x => x.Status)
                .HasMaxLength(20); // Optional: später Enum->string

            builder.Entity<Friendship>()
                .HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // === TimeBlock: typische Filter
            builder.Entity<TimeBlock>()
                .HasIndex(x => new { x.UserId, x.Date });

            // EF Core 8+: Check-Constraint via Table-Builder
            builder.Entity<TimeBlock>()
                .ToTable(t => t.HasCheckConstraint(
                    name: "CK_TimeBlock_EndAfterStart",
                    sql: "\"EndTime\" > \"StartTime\""
                ));

            // === UserTask: häufige Filter + SetNull bei TimeBlock-Löschung
            builder.Entity<UserTask>()
                .HasIndex(x => new { x.UserId, x.IsCompleted, x.DueDate });

            builder.Entity<UserTask>()
                .HasOne(t => t.TimeBlock)
                .WithMany(tb => tb.Tasks)
                .HasForeignKey(t => t.TimeBlockId)
                .OnDelete(DeleteBehavior.SetNull);

            // === TemplateBlock: Check-Constraint ebenfalls via Table-Builder
            builder.Entity<TemplateBlock>()
                .ToTable(t => t.HasCheckConstraint(
                    name: "CK_TemplateBlock_EndAfterStart",
                    sql: "\"EndTime\" > \"StartTime\""
                ));

            // Optional: hilfreich bei Listen/Suchen
            builder.Entity<Template>()
                .HasIndex(x => new { x.UserId, x.CreatedAt });
        }
    }
}
