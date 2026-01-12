using ClipViewer.API.Interfaces;
using Xabe.FFmpeg;

namespace ClipViewer.API.Services;

public class FFMpegService : IFFmpegService
{
    public async Task ConvertToHls(string videoFile, string destinationFolder, CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(destinationFolder);
        var conversion = FFmpeg.Conversions
            .New()
            .AddParameter($"-i {videoFile}")
            .AddParameter(
                "-c:v libx264 -profile:v main -level 4.0 " +
                "-b:v 5000k -maxrate 5500k -bufsize 10000k " +
                "-c:a aac -b:a 128k " +
                "-start_number 0 -hls_time 10 -hls_list_size 0 " +
                "-hls_flags independent_segments -f hls")
            .AddParameter($"{destinationFolder}/index.m3u8");

        await conversion.Start(stoppingToken);
    }

    public async Task GenerateThumbnail(string videoFile, string destinationFile, CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

        var conversion = FFmpeg.Conversions.New()
            .AddParameter($"-i {videoFile}")
            .AddParameter("-vf \"thumbnail,scale=320:-1\" -frames:v 1")
            .AddParameter(destinationFile);

        await conversion.Start(stoppingToken);
    }
}