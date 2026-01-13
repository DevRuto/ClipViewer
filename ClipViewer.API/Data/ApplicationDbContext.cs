using ClipViewer.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // Add your DbSet properties here
    public DbSet<VideoClip> VideoClips => Set<VideoClip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure your entities here
        modelBuilder.Entity<VideoClip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VideoId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SourceVideoFile).IsRequired();
            entity.Property(e => e.Duration).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Add index to VideoId
            entity.HasIndex(e => e.VideoId);
        });
    }
}
