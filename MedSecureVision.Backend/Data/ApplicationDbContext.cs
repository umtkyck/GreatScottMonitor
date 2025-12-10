using Microsoft.EntityFrameworkCore;
using MedSecureVision.Backend.Models;

namespace MedSecureVision.Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<FaceTemplate> FaceTemplates { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Auth0UserId).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.Property(e => e.Status).HasMaxLength(20);
        });

        // FaceTemplate configuration
        modelBuilder.Entity<FaceTemplate>(entity =>
        {
            entity.HasKey(e => e.TemplateId);
            entity.HasIndex(e => e.UserId);
            // Remove database-specific column type for cross-database compatibility
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EventType);
            // Remove database-specific column type for cross-database compatibility
        });
    }
}

