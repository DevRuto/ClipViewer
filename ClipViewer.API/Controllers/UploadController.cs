using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace ClipViewer.API.Controllers;

[Route("api/[controller]")]
public class UploadController(
    IVideoJobQueue videoJobQueue) : ControllerBase
{
    // GET
    public IActionResult Index()
    {
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromHeader(Name = "X-Api-Key")] Guid apiKey)
    {
        // Validate media
        var supportedExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov" };
        if (!supportedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest("Unsupported file type");
        }

        // TODO: Save to file system
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;
        
        // Process file
        var jobId = Guid.NewGuid();
        var filename = await videoJobQueue.EnqueueAsync(new VideoConversionJob("input", "output", jobId));

        return Accepted(new { jobId, filename });
    }
}