using System.Security.Cryptography;
using System.Threading.Channels;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;

namespace ClipViewer.API.Services;

public class VideoJobQueue(
    Channel<VideoConversionJob> channel,
    ILogger<VideoJobQueue> logger) : IVideoJobQueue
{
    public async ValueTask<string> EnqueueAsync(VideoConversionJob job, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Enqueueing job {job}", job);
        await channel.Writer.WriteAsync(job, cancellationToken);
        return $"{GenerateFilename()}{Path.GetExtension(job.InputPath)}";
    }
    
    private static string GenerateFilename(int bytes = 4)
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes))
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "");
}