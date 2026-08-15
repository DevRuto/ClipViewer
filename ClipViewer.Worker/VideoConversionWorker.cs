using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xabe.FFmpeg;

namespace ClipViewer.Worker;

public partial class VideoConversionWorker(
    ILogger<VideoConversionWorker> logger,
    IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                // A fresh scope/DbContext per iteration keeps the change tracker from accumulating
                // every entity ever processed for the lifetime of this long-running service.
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
            catch (Exception e) when (e is not OperationCanceledException)
            {
                LogPollLoopError(logger, e);
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
            Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);

            if (File.Exists(sourceFile))
                File.Delete(sourceFile);

            if (job.EndTime > job.StartTime)
            {
                var tempSource = Path.Combine(job.OutputDirectory, "source", "original",
                    $"{job.VideoId}{Path.GetExtension(job.InputPath)}");

                Directory.CreateDirectory(Path.GetDirectoryName(tempSource)!);
                File.Copy(job.InputPath, tempSource);
                await TrimVideo(tempSource, $"{sourceFile}", job.StartTime, job.EndTime);
                LogTrimVideo(logger, job.JobId, job.VideoId, job.StartTime, job.EndTime);

                try
                {
                    // Delete the untrimmed copy - only sourceFile (the trimmed output) is needed from here on
                    File.Delete(tempSource);
                }
                catch
                {
                    LogUnableToDeleteTempFile(logger, tempSource);
                }
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
                if (progress.TotalLength.TotalSeconds <= 0) return;

                var percent = (int)(Math.Round(progress.Duration.TotalSeconds / progress.TotalLength.TotalSeconds, 2) *
                                    100);
                await _lock.WaitAsync(stoppingToken);
                try
                {
                    job.Progress = percent;
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
                finally
                {
                    _lock.Release();
                }

                LogVideoProgressUpdated(logger, job.VideoId, percent);
            };

            await ffmpegJob.Start(stoppingToken);


            if (!File.Exists(sourceFile)) return;
            // Generate thumbnail
            var thumbnailPath = Path.Combine(job.OutputDirectory, "thumbnails", $"{job.VideoId}.jpg");
            await GenerateThumbnail(sourceFile, thumbnailPath, stoppingToken);

            var dbVideo = await dbContext.VideoClips.FirstOrDefaultAsync(video => video.VideoId == job.VideoId, stoppingToken);
            if (dbVideo is null)
            {
                LogVideoDeletedMidConversion(logger, job.JobId, job.VideoId);
                return;
            }

            dbVideo.Duration = videoInfo.Duration;
            dbVideo.Thumbnail = $"/thumbnails/{job.VideoId}.jpg";
            dbVideo.HlsPlaylistFile = $"/hls/{job.VideoId}/playlist.m3u8";
            dbVideo.Processed = true;
            dbVideo.SizeBytes = GetFileSize(sourceFile) + GetFileSize(thumbnailPath) + GetDirectorySize(hlsPath);
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
        ILogger<VideoConversionWorker> logger, Guid job, string? videoId, string path);

    [LoggerMessage(LogLevel.Error, "Unable to delete temporary file {path}")]
    static partial void LogUnableToDeleteTempFile(ILogger<VideoConversionWorker> logger, string path);

    [LoggerMessage(LogLevel.Information, "Trimmed video for {job} - {videoId} [{start} - {end}]")]
    static partial void LogTrimVideo(
        ILogger<VideoConversionWorker> logger, Guid job, string videoId, int start, int? end);

    [LoggerMessage(LogLevel.Error, "Unable to process video {job}")]
    static partial void LogUnableToProcessVideo(ILogger<VideoConversionWorker> logger, Guid job, Exception e);

    [LoggerMessage(LogLevel.Error, "Error in conversion poll loop")]
    static partial void LogPollLoopError(ILogger<VideoConversionWorker> logger, Exception e);

    [LoggerMessage(LogLevel.Warning, "Video {videoId} for job {job} no longer exists, skipping (likely deleted mid-conversion)")]
    static partial void LogVideoDeletedMidConversion(ILogger<VideoConversionWorker> logger, Guid job, string videoId);

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
            .AddParameter("-preset slow") // Slower preset = better compression for the same quality
            .AddParameter("-crf 23") // Constant-quality target instead of a flat bitrate
            .AddParameter("-maxrate 5000k") // Cap for high-motion/high-res content only
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
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);

        var conversion = FFmpeg.Conversions.New()
            .AddParameter($"-i \"{videoFile}\"")
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

    private static long GetFileSize(string path) =>
        !string.IsNullOrEmpty(path) && File.Exists(path) ? new FileInfo(path).Length : 0;

    private static long GetDirectorySize(string path) =>
        Directory.Exists(path)
            ? new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length)
            : 0;

    // One-off backfill for clips that were processed before VideoClip.SizeBytes existed - resolves
    // the clip's stored relative paths against OutputVideoFolder, unlike the direct absolute paths
    // ProcessAsync already has on hand right after conversion. Invoked via `--backfill-sizes` (see
    // Program.cs); safe to re-run since it just recomputes and overwrites SizeBytes each time.
    public static async Task<int> BackfillClipSizesAsync(
        ApplicationDbContext dbContext, string outputVideoFolder, CancellationToken cancellationToken = default)
    {
        var clips = await dbContext.VideoClips.Where(c => c.Processed).ToListAsync(cancellationToken);

        foreach (var clip in clips)
        {
            var sourceSize = GetFileSize(ResolveOutputPath(outputVideoFolder, clip.SourceVideoFile));
            var thumbnailSize = GetFileSize(ResolveOutputPath(outputVideoFolder, clip.Thumbnail));

            var hlsFolder = string.IsNullOrEmpty(clip.HlsPlaylistFile)
                ? null
                : Path.GetDirectoryName(TrimLeadingSeparators(clip.HlsPlaylistFile));
            var hlsSize = string.IsNullOrEmpty(hlsFolder)
                ? 0
                : GetDirectorySize(Path.Combine(outputVideoFolder, hlsFolder));

            clip.SizeBytes = sourceSize + thumbnailSize + hlsSize;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return clips.Count;
    }

    private static string ResolveOutputPath(string outputVideoFolder, string relativePath) =>
        string.IsNullOrEmpty(relativePath) ? "" : Path.Combine(outputVideoFolder, TrimLeadingSeparators(relativePath));

    private static string TrimLeadingSeparators(string path) =>
        path.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
