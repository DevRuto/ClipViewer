using System.Threading.Channels;
using ClipViewer.API.Models;

namespace ClipViewer.API.Services;

public partial class VideoConversionWorker(
    Channel<VideoConversionJob> channel,
    ILogger<VideoConversionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in channel.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(job, stoppingToken);
        }
    }

    private async Task ProcessAsync(VideoConversionJob job, CancellationToken stoppingToken)
    {
        LogProcessing(logger, job.JobId);
        await Task.Delay(10000, stoppingToken);
        LogFinishedProcessing(logger, job.JobId);
    }

    [LoggerMessage(LogLevel.Information, "Processing video conversion job {job}")]
    static partial void LogProcessing(ILogger<VideoConversionWorker> logger, Guid job);

    [LoggerMessage(LogLevel.Information, "Finished processing video conversion job {job}")]
    static partial void LogFinishedProcessing(ILogger<VideoConversionWorker> logger, Guid job);
}