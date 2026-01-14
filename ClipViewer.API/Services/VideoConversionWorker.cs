using System.Threading.Channels;
using ClipViewer.API.Data;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;

namespace ClipViewer.API.Services;

public partial class VideoConversionWorker(
    ILogger<VideoConversionWorker> logger,
    Channel<VideoConversionJob> channel,
    IFFmpegService ffmpegService,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
            await ProcessAsync(job, dbContext, stoppingToken);
    }

    private async Task ProcessAsync(
        VideoConversionJob job, ApplicationDbContext dbContext,
        CancellationToken stoppingToken)
    {
        LogProcessingStart(logger, job.JobId);

        // This should never happen
        // The queue is setting this
        if (job.VideoId is null)
            return;

        var sourceFile =
            Path.Combine(job.OutputDirectory, "source", $"{job.VideoId}{Path.GetExtension(job.InputPath)}");

        // Copy temp file to output directory
        // Do not delete so we have a source if something goes wrong
        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile));
        File.Copy(job.InputPath, sourceFile);

        var videoInfo = await ffmpegService.GetMediaInfo(sourceFile, stoppingToken);

        // Add to db
        var dbVideo = new VideoClip
        {
            VideoId = job.VideoId,
            Name = job.VideoId,
            SourceVideoFile = $"/source/{job.VideoId}{Path.GetExtension(job.InputPath)}",
            Thumbnail = string.Empty,
            HlsPlaylistFile = string.Empty,
            Processed = false,
            Duration = videoInfo.Duration,
            CreatedAt = DateTime.UtcNow,
            UserId = job.AuthorId
        };
        await dbContext.VideoClips.AddAsync(dbVideo, stoppingToken);
        await dbContext.SaveChangesAsync(stoppingToken);

        // Fix hls path to include file id
        var hlsPath = Path.Combine(job.OutputDirectory, "hls", job.VideoId);
        await ffmpegService.ConvertToHls(sourceFile, hlsPath, stoppingToken);

        // Generate thumbnail
        var thumbnailPath = Path.Combine(job.OutputDirectory, "thumbnails", $"{job.VideoId}.jpg");
        await ffmpegService.GenerateThumbnail(sourceFile, thumbnailPath, stoppingToken);

        dbVideo.Thumbnail = $"/thumbnails/{job.VideoId}.jpg";
        dbVideo.HlsPlaylistFile = $"/hls/{job.VideoId}/playlist.m3u8";
        dbVideo.Processed = true;
        await dbContext.SaveChangesAsync(stoppingToken);

        try
        {
            // Delete temp file
            File.Delete(job.InputPath);
        }
        catch
        {
            LogUnableToDeleteTempFile(logger, job.InputPath);
        }

        LogProcessingFinished(logger, job.JobId, job.VideoId);
    }

    [LoggerMessage(LogLevel.Information, "Processing video conversion job {job}")]
    static partial void LogProcessingStart(ILogger<VideoConversionWorker> logger, Guid job);

    [LoggerMessage(LogLevel.Information, "Finished processing video conversion job {job} - {videoId}")]
    static partial void LogProcessingFinished(ILogger<VideoConversionWorker> logger, Guid job, string videoId);

    [LoggerMessage(LogLevel.Error, "Unable to delete temporary file {path}")]
    static partial void LogUnableToDeleteTempFile(ILogger<VideoConversionWorker> logger, string path);
}
