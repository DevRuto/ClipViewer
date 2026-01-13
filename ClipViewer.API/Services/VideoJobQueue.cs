using System.Security.Cryptography;
using System.Threading.Channels;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;

namespace ClipViewer.API.Services;

public partial class VideoJobQueue(
    Channel<VideoConversionJob> channel,
    ILogger<VideoJobQueue> logger) : IVideoJobQueue
{
    public async ValueTask<string> EnqueueAsync(VideoConversionJob job, CancellationToken cancellationToken = default)
    {
        LogEnqueueingJob(logger, job);
        job.VideoId = GenerateFilename();
        var generatedFilename = $"{job.VideoId}{Path.GetExtension(job.InputPath)}";
        await channel.Writer.WriteAsync(job, cancellationToken);
        return generatedFilename;
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
}