using System.Net;
using ClipViewer.Data;
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
            new PathString("/users"),
            new PathString("/login"),
            new PathString("/upload")
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

                // Default template variables, used for all non-clip SPA routes
                var title = "Ruto's ClipViewer";
                var description = "Ruto's ClipViewer";
                var image = "";
                var url = GetFullUrl(context.Request);

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

                    title = video.Name;
                    image = $"{publicPath}{video.Thumbnail}";
                }

                // Replace template variables
                content = content
                    .Replace("{{TITLE}}", WebUtility.HtmlEncode(title))
                    .Replace("{{DESCRIPTION}}", WebUtility.HtmlEncode(description))
                    .Replace("{{IMAGE}}", WebUtility.HtmlEncode(image))
                    .Replace("{{URL}}", WebUtility.HtmlEncode(url));

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
