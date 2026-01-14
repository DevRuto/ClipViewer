using System.Security.Claims;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipViewer.API.Controllers;

[Route("api/[controller]")]
public class UploadController(
    IConfiguration configuration,
    IVideoJobQueue videoJobQueue)
    : ControllerBase
{
    private static readonly Dictionary<string, string> VideoExtensions = new()
    {
        ["video/mp4"] = ".mp4",
        ["video/webm"] = ".webm",
        ["video/quicktime"] = ".mov",
        ["video/x-msvideo"] = ".avi",
        ["video/x-matroska"] = ".mkv"
    };

    private readonly string _outputVideoFolder = configuration
                                                     .GetSection("UploadOptions")
                                                     .GetSection("OutputVideoFolder").Value
                                                 ?? throw new InvalidOperationException();

    private readonly string _tempVideoFolder = configuration
                                                   .GetSection("UploadOptions")
                                                   .GetSection("TempVideoFolder").Value
                                               ?? throw new InvalidOperationException();

    [HttpPost]
    [Authorize]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(
        [FromHeader(Name = "Content-Type")] string contentType, [FromHeader(Name = "X-Api-Key")] Guid apiKey)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Validate media
        if (!VideoExtensions.TryGetValue(contentType, out var extension))
            return BadRequest("Unsupported video type");

        // Save to temp folder
        var filePath = Path.Combine(_tempVideoFolder, $"{Guid.NewGuid()}{extension}");
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await Request.Body.CopyToAsync(fileStream);

        // Process file
        var jobId = Guid.NewGuid();
        var filename =
            await videoJobQueue.EnqueueAsync(new VideoConversionJob(int.Parse(userId), filePath, _outputVideoFolder,
                jobId));

        return Accepted(new { jobId, filename });
    }
}
