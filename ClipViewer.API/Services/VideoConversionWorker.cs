using System.Threading.Channels;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;

namespace ClipViewer.API.Services;

public partial class VideoConversionWorker(
    Channel<VideoConversionJob> channel,
    ILogger<VideoConversionWorker> logger,
    IFFmpegService ffmpegService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken)) await ProcessAsync(job, stoppingToken);
    }

    private async Task ProcessAsync(VideoConversionJob job, CancellationToken stoppingToken)
    {
        LogProcessingStart(logger, job.JobId);
        // await Task.Delay(10000, stoppingToken);

        // This should never happen
        // The queue is setting this
        if (job.OutputFilePath is null || job.VideoId is null)
            return;

        // Copy temp file to output directory
        // Do not delete so we have a source if something goes wrong
        Directory.CreateDirectory(Path.GetDirectoryName(job.OutputFilePath));
        File.Copy(job.InputPath, job.OutputFilePath);

        // Fix hls path to include file id
        var hlsPath = Path.Combine(job.OutputDirectory, "hls", job.VideoId);
        await ffmpegService.ConvertToHls(job.OutputFilePath, hlsPath, stoppingToken);

        // Generate thumbnail
        var thumbnailPath = Path.Combine(job.OutputDirectory, "thumbnails", $"{job.VideoId}.jpg");
        await ffmpegService.GenerateThumbnail(job.OutputFilePath, thumbnailPath, stoppingToken);

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