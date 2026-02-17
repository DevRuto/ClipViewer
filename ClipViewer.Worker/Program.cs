using ClipViewer.Data;
using ClipViewer.Worker;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<VideoConversionWorker>();

var host = builder.Build();

var outputVideoFolder = builder.Configuration.GetSection("UploadOptions").GetSection("OutputVideoFolder").Value;
var tempVideoFolder = builder.Configuration.GetSection("UploadOptions").GetSection("TempVideoFolder").Value;

// Configure folders
if (outputVideoFolder is null || tempVideoFolder is null)
    throw new InvalidOperationException("OutputVideoFolder or TempVideoFolder is not configured");
if (!Directory.Exists(outputVideoFolder)) Directory.CreateDirectory(outputVideoFolder);
if (!Directory.Exists(tempVideoFolder)) Directory.CreateDirectory(tempVideoFolder);

host.Run();
