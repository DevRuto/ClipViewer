using System.Threading.Channels;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;

namespace ClipViewer.API.Services;

public class VideoJobQueue(
    Channel<VideoConversionJob> channel,
    ILogger<VideoJobQueue> logger) : IVideoJobQueue
{
    public ValueTask EnqueueAsync(VideoConversionJob job, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Enqueueing job {job}", job);
        return channel.Writer.WriteAsync(job, cancellationToken);
    }
}