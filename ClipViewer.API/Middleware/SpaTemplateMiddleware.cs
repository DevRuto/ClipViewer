using System.Net;
using System.Text.RegularExpressions;
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
                var description = "Self-hosted video clip sharing.";
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
                    description = string.IsNullOrWhiteSpace(video.Description)
                        ? $"Watch \"{video.Name}\" on Ruto's ClipViewer"
                        : StripMarkdown(video.Description);
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

    // Descriptions are authored as markdown (rendered client-side via marked); social embeds
    // (Discord/Slack/etc.) only read the raw og:description text, so strip markdown syntax down
    // to plain text for them rather than showing literal "**", "[text](url)", "#", etc.
    private static string StripMarkdown(string markdown)
    {
        var text = markdown;

        text = Regex.Replace(text, "```[a-zA-Z0-9]*\r?\n?([\\s\\S]*?)```", "$1");
        text = Regex.Replace(text, "`([^`]*)`", "$1");
        text = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]*\)", "$1");
        text = Regex.Replace(text, @"\[([^\]]*)\]\([^)]*\)", "$1");
        text = Regex.Replace(text, @"^\s{0,3}#{1,6}\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\s{0,3}>\s?", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"(\*\*\*|___)(.*?)\1", "$2");
        text = Regex.Replace(text, @"(\*\*|__)(.*?)\1", "$2");
        text = Regex.Replace(text, @"(?<!\w)(\*|_)(.*?)\1(?!\w)", "$2");
        text = Regex.Replace(text, "~~(.*?)~~", "$1");
        text = Regex.Replace(text, @"^\s{0,3}[-*+]\s+", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"^\s{0,3}\d+\.\s+", "", RegexOptions.Multiline);

        return text.Trim();
    }
}
