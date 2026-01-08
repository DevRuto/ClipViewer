using System.Threading.Channels;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;
using ClipViewer.API.Services;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

// Download FFmpeg
var ffmpegFolder = Path.Combine(Environment.CurrentDirectory, "FFmpeg");
FFmpeg.SetExecutablesPath(ffmpegFolder);
await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegFolder);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IFFmpegService, FFMpegService>();

builder.Services.AddSingleton(
    Channel.CreateUnbounded<VideoConversionJob>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        }));
builder.Services.AddSingleton<IVideoJobQueue, VideoJobQueue>();
builder.Services.AddHostedService<VideoConversionWorker>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

var outputVideoFolder = builder.Configuration.GetSection("UploadOptions").GetSection("OutputVideoFolder").Value;
var tempVideoFolder = builder.Configuration.GetSection("UploadOptions").GetSection("TempVideoFolder").Value;
if (outputVideoFolder is null || tempVideoFolder is null)
    throw new InvalidOperationException("OutputVideoFolder or TempVideoFolder is not configured");
if (!Directory.Exists(outputVideoFolder)) Directory.CreateDirectory(outputVideoFolder);
if (!Directory.Exists(tempVideoFolder)) Directory.CreateDirectory(tempVideoFolder);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// app.UseHttpsRedirection();

app.UseAuthorization();

// Serve static files from output folder
var outputPath = Path.IsPathRooted(outputVideoFolder) 
    ? outputVideoFolder 
    : Path.Combine(Directory.GetCurrentDirectory(), outputVideoFolder);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(outputPath),
    RequestPath = "/videos"
});

// Block access to temp folder
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/videos/temp"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next();
});

app.MapControllers();

app.Run();