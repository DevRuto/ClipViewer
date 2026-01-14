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
    [RequestFormLimits(MultipartBodyLengthLimit = 500_000_000)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromHeader(Name = "X-Api-Key")] Guid apiKey)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Validate media
        var supportedExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov" };
        if (!supportedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
            return BadRequest("Unsupported file type");

        // Save to temp folder
        var filePath = Path.Combine(_tempVideoFolder, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        // Process file
        var jobId = Guid.NewGuid();
        var filename =
            await videoJobQueue.EnqueueAsync(new VideoConversionJob(int.Parse(userId), filePath, _outputVideoFolder,
                jobId));

        return Accepted(new { jobId, filename });
    }
}
