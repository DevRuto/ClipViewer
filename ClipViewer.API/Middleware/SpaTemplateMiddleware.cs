using ClipViewer.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Middleware;

public class SpaTemplateMiddleware(RequestDelegate next, IWebHostEnvironment env, string spaPath)
{
    private readonly IWebHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        var activeVueRoutes = new[]
        {
            new PathString("/"),
            new PathString("/browse"),
            new PathString("/clips"),
            new PathString("/users")
        };

        // Only process GET requests for the root or other SPA routes
        if (context.Request.Method == "GET" &&
            !Path.HasExtension(context.Request.Path.Value)
            && activeVueRoutes.Any(route => context.Request.Path.StartsWithSegments(route)))
        {
            var filePath = Path.Combine(spaPath, "index.html");
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);

                if (context.Request.Path.StartsWithSegments("/clips", out var remainingPath))
                {
                    if (!remainingPath.HasValue) return;
                    var videoId = remainingPath.Value.TrimStart('/');
                    if (string.IsNullOrEmpty(videoId)) return;

                    var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var config = context.RequestServices.GetRequiredService<IConfiguration>();
                    var publicPath = config.GetSection("UploadOptions").GetSection("PublicFilePath").Value ?? "/files";

                    var video = await db.VideoClips.FirstOrDefaultAsync(video => video.VideoId == videoId);
                    if (video == null) return;

                    // Replace template variables
                    content = content
                        .Replace("{{TITLE}}", video.Name)
                        .Replace("{{DESCRIPTION}}", "Ruto's ClipViewer")
                        .Replace("{{IMAGE}}", $"{publicPath}{video.Thumbnail}")
                        .Replace("{{URL}}", GetFullUrl(context.Request));
                }

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(content);
                return;
            }
        }

        await next(context);
    }

    private static string GetFullUrl(HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
    }
}
