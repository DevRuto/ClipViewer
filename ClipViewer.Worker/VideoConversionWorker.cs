using ClipViewer.Data;
using ClipViewer.Data.Models;
using ClipViewer.Worker.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.Worker;

public partial class VideoConversionWorker(
    ILogger<VideoConversionWorker> logger,
    IFFmpegService ffmpegService,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var job = await dbContext.VideoConversionJobs
                    .OrderBy(j => j.CreatedAt)
                    .FirstOrDefaultAsync(j => j.Status == "Pending", stoppingToken);
                if (job is null)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                ;
                job.Status = "Processing";
                job.StartedAt = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(stoppingToken);
                await ProcessAsync(job, dbContext, stoppingToken);
            }
            finally
            {
                // Sleep for 1 second
                await Task.Delay(1000, stoppingToken);
            }
    }

    private async Task ProcessAsync(
        VideoConversionJob job, ApplicationDbContext dbContext,
        CancellationToken stoppingToken)
    {
        LogProcessingStart(logger, job.JobId);

        // This should never happen
        // The queue is setting this
        if (job.VideoId is null || !File.Exists(job.InputPath))
            return;

        var sourceFile =
            Path.Combine(job.OutputDirectory, "source", $"{job.VideoId}{Path.GetExtension(job.InputPath)}");
        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile));

        if (job.EndTime > job.StartTime)
        {
            var tempSource = Path.Combine(job.OutputDirectory, "source", "original",
                $"{job.VideoId}{Path.GetExtension(job.InputPath)}");

            Directory.CreateDirectory(Path.GetDirectoryName(tempSource));
            File.Copy(job.InputPath, tempSource);
            await ffmpegService.TrimVideo(tempSource, sourceFile, job.StartTime, job.EndTime);
            LogTrimVideo(logger, job.JobId, job.VideoId, job.StartTime, job.EndTime);
        }
        else
        {
            File.Copy(job.InputPath, sourceFile);
        }

        if (!File.Exists(sourceFile)) return;
        var videoInfo = await ffmpegService.GetMediaInfo(sourceFile, stoppingToken);

        // Fix hls path to include file id
        var hlsPath = Path.Combine(job.OutputDirectory, "hls", job.VideoId);
        await ffmpegService.ConvertToHls(sourceFile, hlsPath, stoppingToken);

        if (!File.Exists(sourceFile)) return;
        // Generate thumbnail
        var thumbnailPath = Path.Combine(job.OutputDirectory, "thumbnails", $"{job.VideoId}.jpg");
        await ffmpegService.GenerateThumbnail(sourceFile, thumbnailPath, stoppingToken);

        var dbVideo = await dbContext.VideoClips.FirstAsync(video => video.VideoId == job.VideoId, stoppingToken);
        dbVideo.Duration = videoInfo.Duration;
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

    [LoggerMessage(LogLevel.Information, "Trimmed video for {job} - {videoId} [{start} - {end}]")]
    static partial void LogTrimVideo(
        ILogger<VideoConversionWorker> logger, Guid job, string videoId, int start, int? end);
}
