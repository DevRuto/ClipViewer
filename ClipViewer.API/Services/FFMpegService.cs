using ClipViewer.API.Interfaces;
using Xabe.FFmpeg;

namespace ClipViewer.API.Services;

public class FFMpegService : IFFmpegService
{
    public async Task ConvertToHls(string m3u8Playlist, string destinationFolder, CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(destinationFolder);
        var conversion = FFmpeg.Conversions
            .New()
            .AddParameter($"-i {m3u8Playlist}")
            .AddParameter(
                "-c:v libx264 -profile:v main -level 4.0 " +
                "-b:v 5000k -maxrate 5500k -bufsize 10000k " +
                "-c:a aac -b:a 128k " +
                "-start_number 0 -hls_time 10 -hls_list_size 0 " +
                "-hls_flags independent_segments -f hls")
            .AddParameter($"{destinationFolder}/index.m3u8");

        await conversion.Start(stoppingToken);
    }
}