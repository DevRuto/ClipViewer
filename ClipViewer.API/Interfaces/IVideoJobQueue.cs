using ClipViewer.API.Models;

namespace ClipViewer.API.Interfaces;

public interface IVideoJobQueue
{
    ValueTask EnqueueAsync(VideoConversionJob job, CancellationToken cancellationToken = default);
}