namespace ClipViewer.API.Interfaces;

public interface IFFmpegService
{
    Task ConvertToHls(string m3u8Playlist, string destinationFolder, CancellationToken stoppingToken);
}