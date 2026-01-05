using System.Threading.Channels;
using ClipViewer.API.Models;

namespace ClipViewer.API.Services;

public class VideoConversionWorker(
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
        logger.LogInformation("Processing video conversion job {job}", job.JobId);
        await Task.Delay(10000);
        logger.LogInformation("Finished processing video conversion job {job}", job.JobId);
    }
}