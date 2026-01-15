using System.Security.Cryptography;
using System.Threading.Channels;
using ClipViewer.API.Data;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;

namespace ClipViewer.API.Services;

public partial class VideoJobQueue(
    IServiceScopeFactory serviceScopeFactory,
    Channel<VideoConversionJob> channel,
    ILogger<VideoJobQueue> logger) : IVideoJobQueue
{
    public async ValueTask<string> EnqueueAsync(VideoConversionJob job, CancellationToken stoppingToken = default)
    {
        LogEnqueueingJob(logger, job);
        job.VideoId = GenerateFilename();

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Add to db
        var dbVideo = new VideoClip
        {
            VideoId = job.VideoId,
            Name = string.IsNullOrEmpty(job.Name) ? job.VideoId : job.Name,
            SourceVideoFile = $"/source/{job.VideoId}{Path.GetExtension(job.InputPath)}",
            Thumbnail = string.Empty,
            HlsPlaylistFile = string.Empty,
            Processed = false,
            CreatedAt = DateTime.UtcNow,
            UserId = job.AuthorId
        };
        await dbContext.VideoClips.AddAsync(dbVideo, stoppingToken);
        await dbContext.SaveChangesAsync(stoppingToken);

        await channel.Writer.WriteAsync(job, stoppingToken);
        return job.VideoId;
    }

    private static string GenerateFilename(int bytes = 4)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes))
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
    }

    [LoggerMessage(LogLevel.Information, "Enqueueing job {job}")]
    static partial void LogEnqueueingJob(ILogger<VideoJobQueue> logger, VideoConversionJob job);
}
