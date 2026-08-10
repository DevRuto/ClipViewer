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

        if (!string.IsNullOrEmpty(name) && name.Length > 200)
            return BadRequest("Name must be 200 characters or fewer");

        if (_maxUploadSizeBytes is > 0 && Request.ContentLength > _maxUploadSizeBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                $"File exceeds maximum upload size of {_maxUploadSizeBytes} bytes");

        // Save to temp folder, enforcing the cap on actual bytes written since Content-Length can be
        // absent (e.g. chunked transfer encoding) and isn't trustworthy on its own.
        var filePath = Path.Combine(_tempVideoFolder, $"{Guid.NewGuid()}{extension}");
        var tooLarge = false;
        await using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            var buffer = new byte[81920];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await Request.Body.ReadAsync(buffer)) > 0)
            {
                totalBytesRead += bytesRead;
                if (_maxUploadSizeBytes is > 0 && totalBytesRead > _maxUploadSizeBytes)
                {
                    tooLarge = true;
                    break;
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }
        }

        if (tooLarge)
        {
            System.IO.File.Delete(filePath);
            return StatusCode(StatusCodes.Status413PayloadTooLarge,
                $"File exceeds maximum upload size of {_maxUploadSizeBytes} bytes");
        }

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
