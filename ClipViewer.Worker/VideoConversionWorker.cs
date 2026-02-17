using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xabe.FFmpeg;

namespace ClipViewer.Worker;

public partial class VideoConversionWorker(
    ILogger<VideoConversionWorker> logger,
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

        try
        {
            // This should never happen
            // The queue is setting this
            if (job.VideoId is null || !File.Exists(job.InputPath))
            {
                LogNoProcessingFinished(logger, job.JobId, job.VideoId, job.InputPath);
                return;
            }

            var sourceFile =
                Path.Combine(job.OutputDirectory, "source", $"{job.VideoId}{Path.GetExtension(job.InputPath)}");
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFile));

            if (File.Exists(sourceFile))
                File.Delete(sourceFile);

            if (job.EndTime > job.StartTime)
            {
                var tempSource = Path.Combine(job.OutputDirectory, "source", "original",
                    $"{job.VideoId}{Path.GetExtension(job.InputPath)}");

                Directory.CreateDirectory(Path.GetDirectoryName(tempSource));
                File.Copy(job.InputPath, tempSource);
                await TrimVideo(tempSource, $"{sourceFile}", job.StartTime, job.EndTime);
                LogTrimVideo(logger, job.JobId, job.VideoId, job.StartTime, job.EndTime);
            }
            else
            {
                File.Copy(job.InputPath, sourceFile);
            }

            if (!File.Exists(sourceFile)) return;
            var videoInfo = await GetMediaInfo(sourceFile, stoppingToken);

            // Fix hls path to include file id
            var hlsPath = Path.Combine(job.OutputDirectory, "hls", job.VideoId);
            var ffmpegJob = GenerateHlsTask(sourceFile, hlsPath);

            ffmpegJob.OnProgress += async (_, progress) =>
            {
                var percent = (int)(Math.Round(progress.Duration.TotalSeconds / progress.TotalLength.TotalSeconds, 2) *
                                    100);
                job.Progress = percent;
                await dbContext.SaveChangesAsync(stoppingToken);
                LogVideoProgressUpdated(logger, job.VideoId, percent);
            };

            await ffmpegJob.Start(stoppingToken);


            if (!File.Exists(sourceFile)) return;
            // Generate thumbnail
            var thumbnailPath = Path.Combine(job.OutputDirectory, "thumbnails", $"{job.VideoId}.jpg");
            await GenerateThumbnail(sourceFile, thumbnailPath, stoppingToken);

            var dbVideo = await dbContext.VideoClips.FirstAsync(video => video.VideoId == job.VideoId, stoppingToken);
            dbVideo.Duration = videoInfo.Duration;
            dbVideo.Thumbnail = $"/thumbnails/{job.VideoId}.jpg";
            dbVideo.HlsPlaylistFile = $"/hls/{job.VideoId}/playlist.m3u8";
            dbVideo.Processed = true;
            job.CompletedAt = DateTime.UtcNow;
            job.Status = "Completed";

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
        catch (Exception e)
        {
            job.Status = "Error";
            LogUnableToProcessVideo(logger, job.JobId, e);
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }

    [LoggerMessage(LogLevel.Information, "Processing video conversion job {job}")]
    static partial void LogProcessingStart(ILogger<VideoConversionWorker> logger, Guid job);

    [LoggerMessage(LogLevel.Information, "Finished processing video conversion job {job} - {videoId}")]
    static partial void LogProcessingFinished(ILogger<VideoConversionWorker> logger, Guid job, string videoId);

    [LoggerMessage(LogLevel.Information, "No processing occured {job} - {videoId} - {path}")]
    static partial void LogNoProcessingFinished(
        ILogger<VideoConversionWorker> logger, Guid job, string videoId, string path);

    [LoggerMessage(LogLevel.Error, "Unable to delete temporary file {path}")]
    static partial void LogUnableToDeleteTempFile(ILogger<VideoConversionWorker> logger, string path);

    [LoggerMessage(LogLevel.Information, "Trimmed video for {job} - {videoId} [{start} - {end}]")]
    static partial void LogTrimVideo(
        ILogger<VideoConversionWorker> logger, Guid job, string videoId, int start, int? end);

    [LoggerMessage(LogLevel.Error, "Unable to process video {job}\n{e}")]
    static partial void LogUnableToProcessVideo(ILogger<VideoConversionWorker> logger, Guid job, Exception e);

    [LoggerMessage(LogLevel.Information, "Video progress updated {videoId} - {percent}%")]
    static partial void LogVideoProgressUpdated(ILogger<VideoConversionWorker> logger, string videoId, int percent);

    private static IConversion GenerateHlsTask(
        string videoFile, string destinationFolder)
    {
        Directory.CreateDirectory(destinationFolder);
        var conversion = FFmpeg.Conversions
            .New()
            .AddParameter($"-i \"{videoFile}\"") // Input file
            .AddParameter("-c:v libx264") // Video codec
            .AddParameter("-profile:v main") // H.264 profile
            .AddParameter("-level 4.0") // H.264 level
            .AddParameter("-b:v 5000k") // Video bitrate
            .AddParameter("-maxrate 5500k") // Maximum bitrate
            .AddParameter("-bufsize 10000k") // Buffer size
            .AddParameter("-c:a aac") // Audio codec
            .AddParameter("-b:a 128k") // Audio bitrate
            .AddParameter("-start_number 0") // Start segment number
            .AddParameter("-hls_time 10") // Segment duration in seconds
            .AddParameter("-hls_list_size 0") // Playlist size limit
            .AddParameter("-hls_flags independent_segments") // Independent segment decoding
            .AddParameter("-f hls") // Output format
            .AddParameter($"\"{destinationFolder}/playlist.m3u8\""); // Output playlist path;

        return conversion;
    }


    private static Task GenerateThumbnail(string videoFile, string destinationFile, CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

        var conversion = FFmpeg.Conversions.New()
            .AddParameter($"-i {videoFile}")
            .AddParameter("-vf \"thumbnail,scale=640:-1\" -frames:v 1")
            .AddParameter(destinationFile);

        return conversion.Start(stoppingToken);
    }

    private static Task<IMediaInfo> GetMediaInfo(string videoFile, CancellationToken stoppingToken)
    {
        return FFmpeg.GetMediaInfo(videoFile, stoppingToken);
    }

    private static async Task TrimVideo(
        string videoFilePath, string destinationFile, int startTime = 0, int? endTime = null)
    {
        var conversion = FFmpeg.Conversions.New()
            .AddParameter($"-i \"{videoFilePath}\"") // input
            .AddParameter($"-ss {startTime}") // start (seconds)
            .AddParameter($"-to {endTime}") // end (absolute seconds)
            .AddParameter("-c:v libx264") // re-encode video
            .AddParameter("-c:a aac") // re-encode audio
            .AddParameter("-movflags +faststart") // web-friendly
            .SetOutput(destinationFile);

        await conversion.Start();
    }
}
