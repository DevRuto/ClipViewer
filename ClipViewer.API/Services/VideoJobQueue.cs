using System.Security.Cryptography;
using ClipViewer.API.Interfaces;
using ClipViewer.Data;
using ClipViewer.Data.Models;

namespace ClipViewer.API.Services;

public partial class VideoJobQueue(
    IServiceScopeFactory serviceScopeFactory,
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
            Id = Guid.NewGuid(),
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

        // Add job
        job.VideoClipId = dbVideo.Id;
        await dbContext.VideoConversionJobs.AddAsync(job, stoppingToken);

        await dbContext.SaveChangesAsync(stoppingToken);

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
