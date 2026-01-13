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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSeeding((context, _) =>
        {
            var db = context as ApplicationDbContext;
            var videos = db.VideoClips.FirstOrDefault();
            if (videos != null) return;
            for (var i = 0; i < 3; i++)
                db.VideoClips.Add(
                    new VideoClip
                    {
                        VideoId = "testvid" + i,
                        Name = $"Test Video {i}",
                        SourceVideoFile = "/source/e42Wkw.mp4",
                        Thumbnail = "/thumbnails/test.jpg",
                        HlsPlaylistFile = "/hls/e42Wkw/playlist.m3u8",
                        Duration = TimeSpan.FromSeconds(10),
                        CreatedAt = DateTime.Now,
                        Processed = true
                    });
            db.VideoClips.Add(
                new VideoClip
                {
                    VideoId = "mkvtest",
                    Name = "Test mkv",
                    SourceVideoFile = "/source/pOMmg.mkv",
                    Thumbnail = "/thumbnails/pOMmg.jpg",
                    HlsPlaylistFile = "/hls/pOMmg/playlist.m3u8",
                    Duration = TimeSpan.FromSeconds(20.3340000),
                    CreatedAt = DateTime.Now,
                    Processed = true
                });
            context.SaveChanges();
        });
    }
}
