using ClipViewer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<VideoClip> VideoClips { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<VideoConversionJob> VideoConversionJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.ApiKey).IsRequired();
            entity.HasIndex(e => e.ApiKey).IsUnique();
            entity.Property(e => e.Role).IsRequired().HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(UserRole.User);
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Configure VideoClip entity
        modelBuilder.Entity<VideoClip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VideoId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SourceVideoFile).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UserId).IsRequired();

            // Configure relationship with User
            entity.HasOne(v => v.User)
                .WithMany(u => u.VideoClips)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.SourceVideoFile).IsRequired();
            entity.Property(e => e.Duration).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Add index to VideoId
            entity.HasIndex(e => e.VideoId).IsUnique();

            // GIN index for tag containment/filtering queries
            entity.HasIndex(e => e.Tags).HasMethod("gin");
        });

        // Configure VideoConversionJob entity
        modelBuilder.Entity<VideoConversionJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VideoClipId).IsRequired();
            entity.Property(e => e.InputPath).IsRequired();
            entity.Property(e => e.OutputDirectory).IsRequired();
            entity.Property(e => e.JobId).IsRequired();
            entity.Property(e => e.AuthorId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            // Configure relationship with VideoClip
            entity.HasOne(v => v.VideoClip)
                .WithMany()
                .HasForeignKey(v => v.VideoClipId)
                .OnDelete(DeleteBehavior.Cascade);

            // Add indexes
            entity.HasIndex(e => e.JobId).IsUnique();
            entity.HasIndex(e => e.VideoClipId);
            // Serves the worker's polling query: WHERE Status = 'Pending' ORDER BY CreatedAt
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSeeding((context, _) =>
        {
            var db = context as ApplicationDbContext;
            var videos = db.VideoClips.FirstOrDefault();
            if (videos != null) return;

            context.SaveChanges();
        });
    }
}
