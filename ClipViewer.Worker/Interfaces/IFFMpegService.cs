using Xabe.FFmpeg;

namespace ClipViewer.Worker.Interfaces;

public interface IFFmpegService
{
    Task ConvertToHls(
        string videoFilePath, string destinationFolder,
        CancellationToken stoppingToken = default);

    Task TrimVideo(string videoFilePath, string destinationFile, int startTime = 0, int? endTime = null);

    Task GenerateThumbnail(string videoFile, string destinationFile, CancellationToken stoppingToken);
    Task<IMediaInfo> GetMediaInfo(string videoFile, CancellationToken stoppingToken);
}
