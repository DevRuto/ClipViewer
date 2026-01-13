using ClipViewer.API.Models;

namespace ClipViewer.API.Interfaces;

public interface IVideoJobQueue
{
    ValueTask<string> EnqueueAsync(VideoConversionJob job, CancellationToken cancellationToken = default);
}
