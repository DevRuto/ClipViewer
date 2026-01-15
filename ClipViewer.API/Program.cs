using System.Text;
using System.Threading.Channels;
using ClipViewer.API.Data;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Middleware;
using ClipViewer.API.Models;
using ClipViewer.API.Models.Auth;
using ClipViewer.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
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

// Add JWT settings from configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Add authentication services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Add authentication and authorization
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Secret"]))
        };
    })
    .AddPolicyScheme("JwtOrApiKey", "JWT or API Key", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                return JwtBearerDefaults.AuthenticationScheme;
            return "ApiKey";
        };
    });

// Configure authorization
builder.Services.AddAuthorization(options =>
{
    var defaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ApiKey")
        .RequireAuthenticatedUser()
        .Build();
    options.DefaultPolicy = defaultPolicy;
});

// Configure database based on environment
if (builder.Environment.IsDevelopment() || builder.Environment.IsProduction())
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
else
    // Use in-memory database for testing
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("ClipViewerDb"));

var app = builder.Build();

var outputVideoFolder = builder.Configuration.GetSection("UploadOptions").GetSection("OutputVideoFolder").Value;
var tempVideoFolder = builder.Configuration.GetSection("UploadOptions").GetSection("TempVideoFolder").Value;
var publicFilePath = builder.Configuration.GetSection("UploadOptions").GetSection("PublicFilePath").Value;

// Configure folders
if (outputVideoFolder is null || tempVideoFolder is null)
    throw new InvalidOperationException("OutputVideoFolder or TempVideoFolder is not configured");
if (!Directory.Exists(outputVideoFolder)) Directory.CreateDirectory(outputVideoFolder);
if (!Directory.Exists(tempVideoFolder)) Directory.CreateDirectory(tempVideoFolder);

// Create database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
else
    // Add SPA template middleware
    app.UseMiddleware<SpaTemplateMiddleware>(spaPath);

// app.UseHttpsRedirection();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost
});

app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Serve static files from output folder
var outputPath = Path.IsPathRooted(outputVideoFolder)
    ? outputVideoFolder
    : Path.Combine(Directory.GetCurrentDirectory(), outputVideoFolder);

var contentTypeProvider = new FileExtensionContentTypeProvider
{
    Mappings =
    {
        [".mp4"] = "video/mp4",
        [".mov"] = "video/mov",
        [".m3u8"] = "application/x-mpegURL",
        [".mkv"] = "video/x-matroska"
    }
};

app.UseWhen(
    context => context.Request.Path.StartsWithSegments(publicFilePath),
    clipBuilder =>
    {
        clipBuilder.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(outputPath),
            RequestPath = publicFilePath,
            ContentTypeProvider = contentTypeProvider
        });
    });

app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"),
    apiBuilder =>
    {
        // Require authorization for all API endpoints except auth endpoints
        apiBuilder.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    });

// Serve SPA files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(spaPath)
});

app.UseSpa(spaBuilder =>
{
    spaBuilder.Options.SourcePath = spaPath;

    if (app.Environment.IsDevelopment())
        spaBuilder.UseProxyToSpaDevelopmentServer("http://localhost:5173");
});

app.Run();
