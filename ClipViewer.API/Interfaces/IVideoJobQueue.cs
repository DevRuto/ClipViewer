using ClipViewer.Data.Models;

namespace ClipViewer.API.Interfaces;

public interface IVideoJobQueue
{
    /// <summary>
    ///     Asynchronously enqueues a video job for processing.
    /// </summary>
    /// <param name="job">The video conversion job to enqueue.</param>
    /// <param name="stoppingToken">
    ///     A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.
    /// </param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains the generated filename for the
    ///     video.
    /// </returns>
    ValueTask<string> EnqueueAsync(VideoConversionJob job, CancellationToken stoppingToken = default);
}
