using System.Threading.Channels;
using ClipViewer.API.Data;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;
using ClipViewer.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

// Download FFmpeg
var ffmpegFolder = Path.Combine(Environment.CurrentDirectory, "FFmpeg");
FFmpeg.SetExecutablesPath(ffmpegFolder);
await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, ffmpegFolder);

var spaPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "clipviewer.vue", "dist");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSpaStaticFiles(options => options.RootPath = spaPath);

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

// Add DbContext with in-memory database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("ClipViewerDb"));

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

app.UseRouting();
app.UseAuthorization();

// Serve static files from output folder
var outputPath = Path.IsPathRooted(outputVideoFolder)
    ? outputVideoFolder
    : Path.Combine(Directory.GetCurrentDirectory(), outputVideoFolder);

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/clips") &&
               !context.Request.Path.StartsWithSegments("/clips/temp"),
    clipBuilder =>
    {
        clipBuilder.UseStaticFiles(new StaticFileOptions
            { FileProvider = new PhysicalFileProvider(outputPath), RequestPath = "/clips" });
    });

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"),
    apiBuilder => { apiBuilder.UseEndpoints(endpoints => { endpoints.MapControllers(); }); });


// Serve SPA files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(spaPath)
});

app.UseSpa(spaBuilder =>
{
    spaBuilder.Options.SourcePath = spaPath;
    spaBuilder.Options.DefaultPageStaticFileOptions = new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(spaPath)
    };

    if (app.Environment.IsDevelopment()) spaBuilder.UseProxyToSpaDevelopmentServer("http://localhost:5173");
});

// Handle SPA fallback routing
// app.Use(async (context, next) =>
// {
//     Console.WriteLine(context.Request.Path);
//     if (context.Request.Path.StartsWithSegments("/clips/temp"))
//     {
//         context.Response.StatusCode = StatusCodes.Status404NotFound;
//         return;
//     }
//
//     // if (!context.Request.Path.StartsWithSegments("/clips") &&
//     //     !context.Request.Path.StartsWithSegments("/api"))
//     context.Request.Path = "/index.html";
//     await next();
// });
//
// app.MapControllers();

app.Run();