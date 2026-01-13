using ClipViewer.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<VideoClip> VideoClips { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;

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

            // 29f16672-c831-4c02-affa-060885ed2de0
            var user = db.Users.Add(new User
            {
                Username = "ruto",
                ApiKey = Guid.Parse("29f16672-c831-4c02-affa-060885ed2de0"),
                CreatedAt = DateTime.UtcNow
            });
            for (var i = 0; i < 3; i++)
                user.Entity.VideoClips.Add(
                    new VideoClip
                    {
                        VideoId = "testvid" + i,
                        Name = $"Test Video {i}",
                        SourceVideoFile = "/source/e42Wkw.mp4",
                        Thumbnail = "/thumbnails/test.jpg",
                        HlsPlaylistFile = "/hls/e42Wkw/playlist.m3u8",
                        Duration = TimeSpan.FromSeconds(10),
                        CreatedAt = DateTime.UtcNow,
                        Processed = true
                    });
            user.Entity.VideoClips.Add(
                new VideoClip
                {
                    VideoId = "mkvtest",
                    Name = "Test mkv",
                    SourceVideoFile = "/source/pOMmg.mkv",
                    Thumbnail = "/thumbnails/pOMmg.jpg",
                    HlsPlaylistFile = "/hls/pOMmg/playlist.m3u8",
                    Duration = TimeSpan.FromSeconds(598),
                    CreatedAt = DateTime.UtcNow,
                    Processed = true
                });
            context.SaveChanges();
        });
    }
}
