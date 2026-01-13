namespace ClipViewer.API.Middleware;

public class SpaTemplateMiddleware(RequestDelegate next, IWebHostEnvironment env, string spaPath)
{
    private readonly IWebHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        var activeVueRoutes = new[] { "/", "/clips" };
        // Only process GET requests for the root or other SPA routes
        if (context.Request.Method == "GET" &&
            !Path.HasExtension(context.Request.Path.Value)
            && activeVueRoutes.Contains(context.Request.Path.Value))
        {
            var filePath = Path.Combine(spaPath, "index.html");
            if (File.Exists(filePath))
            {
                var content = await File.ReadAllTextAsync(filePath);

                // Replace template variables
                content = content
                    .Replace("{{TITLE}}", "ClipViewer - Your Video Clips")
                    .Replace("{{DESCRIPTION}}", "View and manage your video clips with ClipViewer")
                    .Replace("{{IMAGE}}", "/images/og-image.jpg");

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(content);
                return;
            }
        }

        await next(context);
    }
}
