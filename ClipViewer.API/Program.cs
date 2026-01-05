using System.Threading.Channels;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;
using ClipViewer.API.Services;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

var ffmpegFolder = Path.Combine(Environment.CurrentDirectory, "FFmpeg");
FFmpeg.SetExecutablesPath(ffmpegFolder);
await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegFolder);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IFFmpegService, FFMpegService>();

builder.Services.AddSingleton(
    Channel.CreateBounded<VideoConversionJob>(
        new BoundedChannelOptions(50)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        }));
builder.Services.AddSingleton<IVideoJobQueue, VideoJobQueue>();
builder.Services.AddHostedService<VideoConversionWorker>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
