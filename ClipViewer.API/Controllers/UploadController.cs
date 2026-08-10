using System.Security.Claims;
using ClipViewer.API.Interfaces;
using ClipViewer.Data.Models;
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

    private readonly long? _maxUploadSizeBytes = configuration
        .GetSection("UploadOptions")
        .GetValue<long?>("MaxUploadSizeBytes");

    [HttpPost]
    [Authorize(AuthenticationSchemes = "JwtOrApiKey")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(
        [FromHeader(Name = "Content-Type")] string contentType,
        [FromQuery] string? name = "",
        [FromQuery] int? startTime = null,
        [FromQuery] int? endTime = null)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return BadRequest("Unable to get user info");

        // Validate media
        if (!VideoExtensions.TryGetValue(contentType, out var extension))
            return BadRequest("Unsupported video type");

        if (_maxUploadSizeBytes is > 0 && Request.ContentLength > _maxUploadSizeBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                $"File exceeds maximum upload size of {_maxUploadSizeBytes} bytes");

        // Save to temp folder
        var filePath = Path.Combine(_tempVideoFolder, $"{Guid.NewGuid()}{extension}");
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await Request.Body.CopyToAsync(fileStream);

        // Process file
        var jobId = Guid.NewGuid();
        var conversionJob = new VideoConversionJob(userId, filePath, _outputVideoFolder,
            jobId);

        if (endTime is > 0 && endTime > startTime)
        {
            conversionJob.StartTime = startTime.Value;
            conversionJob.EndTime = endTime.Value;
        }

        if (!string.IsNullOrEmpty(name))
            conversionJob.Name = name;
        var videoId =
            await videoJobQueue.EnqueueAsync(conversionJob);


        var filename = $"{videoId}{extension}";
        return Accepted(new { jobId, filename, videoId });
    }
}
