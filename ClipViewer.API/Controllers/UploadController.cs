using ClipViewer.API.Interfaces;
using ClipViewer.API.Models;
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
    public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromHeader(Name = "X-Api-Key")] Guid apiKey)
    {
        // Validate media
        // var supportedExtensions = new[] { ".mp4", ".mkv", ".avi", ".mov" };
        // if (!supportedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
        //     return BadRequest("Unsupported file type");

        // Save to temp folder
        var filePath = Path.Combine(_tempVideoFolder, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fileStream);

        // Process file
        var jobId = Guid.NewGuid();
        var filename = await videoJobQueue.EnqueueAsync(new VideoConversionJob(filePath, _outputVideoFolder, jobId));

        return Accepted(new { jobId, filename });
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Hello World!");
    }
}