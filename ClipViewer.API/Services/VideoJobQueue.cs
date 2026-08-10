using System.Security.Cryptography;
using ClipViewer.API.Interfaces;
using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ClipViewer.API.Services;

public partial class VideoJobQueue(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<VideoJobQueue> logger) : IVideoJobQueue
{
    private const int MaxVideoIdAttempts = 5;

    public async ValueTask<string> EnqueueAsync(VideoConversionJob job, CancellationToken stoppingToken = default)
    {
        LogEnqueueingJob(logger, job);

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        for (var attempt = 1; attempt <= MaxVideoIdAttempts; attempt++)
        {
            job.VideoId = GenerateFilename();

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

            try
            {
                await dbContext.SaveChangesAsync(stoppingToken);
                return job.VideoId;
            }
            catch (DbUpdateException e) when (attempt < MaxVideoIdAttempts &&
                                               e.InnerException is PostgresException { SqlState: "23505" })
            {
                // VideoId collision (2^32 keyspace) - detach the failed entities and retry with a new one
                dbContext.Entry(dbVideo).State = EntityState.Detached;
                dbContext.Entry(job).State = EntityState.Detached;
                LogVideoIdCollision(logger, job.VideoId, attempt);
            }
        }

        throw new InvalidOperationException(
            $"Unable to generate a unique VideoId after {MaxVideoIdAttempts} attempts");
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

    [LoggerMessage(LogLevel.Warning, "VideoId {videoId} collided on attempt {attempt}, retrying")]
    static partial void LogVideoIdCollision(ILogger<VideoJobQueue> logger, string videoId, int attempt);
}
