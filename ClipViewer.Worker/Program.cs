using ClipViewer.Data;
using ClipViewer.Worker;
using ClipViewer.Worker.Interfaces;
using ClipViewer.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

// Download FFmpeg
var ffmpegFolder = Path.Combine(Environment.CurrentDirectory, "FFmpeg");
FFmpeg.SetExecutablesPath(ffmpegFolder);
await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegFolder);

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IFFmpegService, FFMpegService>();

builder.Services.AddHostedService<VideoConversionWorker>();

var host = builder.Build();
host.Run();
