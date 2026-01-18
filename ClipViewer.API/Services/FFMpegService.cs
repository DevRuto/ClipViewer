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
            .AddParameter($"-i \"{videoFile}\"") // Input file
            .AddParameter("-c:v libx264") // Video codec
            .AddParameter("-profile:v main") // H.264 profile
            .AddParameter("-level 4.0") // H.264 level
            .AddParameter("-b:v 5000k") // Video bitrate
            .AddParameter("-maxrate 5500k") // Maximum bitrate
            .AddParameter("-bufsize 10000k") // Buffer size
            .AddParameter("-c:a aac") // Audio codec
            .AddParameter("-b:a 128k") // Audio bitrate
            .AddParameter("-start_number 0") // Start segment number
            .AddParameter("-hls_time 10") // Segment duration in seconds
            .AddParameter("-hls_list_size 0") // Playlist size limit
            .AddParameter("-hls_flags independent_segments") // Independent segment decoding
            .AddParameter("-f hls") // Output format
            .AddParameter($"\"{destinationFolder}/playlist.m3u8\""); // Output playlist path;

        await conversion.Start(stoppingToken);
    }

    public async Task GenerateThumbnail(string videoFile, string destinationFile, CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

        var conversion = FFmpeg.Conversions.New()
            .AddParameter($"-i {videoFile}")
            .AddParameter("-vf \"thumbnail,scale=640:-1\" -frames:v 1")
            .AddParameter(destinationFile);

        await conversion.Start(stoppingToken);
    }

    public Task<IMediaInfo> GetMediaInfo(string videoFile, CancellationToken stoppingToken)
    {
        return FFmpeg.GetMediaInfo(videoFile, stoppingToken);
    }
}
