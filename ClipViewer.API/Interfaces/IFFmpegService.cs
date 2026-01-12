namespace ClipViewer.API.Interfaces;

public interface IFFmpegService
{
    Task ConvertToHls(string videoFile, string destinationFolder, CancellationToken stoppingToken);
    Task GenerateThumbnail(string videoFile, string destinationFile, CancellationToken stoppingToken);
}