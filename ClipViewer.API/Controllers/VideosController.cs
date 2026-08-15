using System.Security.Claims;
using ClipViewer.API.Models;
using ClipViewer.API.Models.DTOs;
using ClipViewer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VideosController(
    IConfiguration configuration,
    ApplicationDbContext context,
    ILogger<VideosController> logger) : ControllerBase
{
    private readonly string _outputVideoFolder = configuration
                                                     .GetSection("UploadOptions")
                                                     .GetSection("OutputVideoFolder").Value
                                                 ?? throw new InvalidOperationException();

    private readonly string _publicFilePath =
        configuration.GetSection("UploadOptions").GetSection("PublicFilePath").Value ?? "/files";

    [HttpGet]
    public async Task<ActionResult<List<VideoClipDto>>> GetVideoList([FromQuery] string user = "")
    {
        var currentUser = User.FindFirst(ClaimTypes.Name)?.Value;

        var videoQuery = context.VideoClips
            .Include(v => v.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(user))
        {
            videoQuery = videoQuery.Where(v =>
                EF.Functions.ILike(v.User.Username, user));

            // Only the owner of the requested user's videos can see their Unlisted ones
            if (!string.Equals(currentUser, user, StringComparison.OrdinalIgnoreCase))
                videoQuery = videoQuery.Where(v => !v.Unlisted);
        }
        else
        {
            // No user filter → only public videos
            videoQuery = videoQuery.Where(v => !v.Unlisted);
        }

        var videos = await videoQuery.OrderByDescending(video => video.CreatedAt).ToListAsync();
        return videos.Select(v => VideoClipDto.FromEntity(v, _publicFilePath)).ToList();
    }

    [HttpGet("{videoId}")]
    public async Task<ActionResult<VideoClipDto>> GetVideo(string videoId)
    {
        var video = await context.VideoClips
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.VideoId == videoId);
        if (video == null) return NotFound();

        var latestJob = await context.VideoConversionJobs.OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(job => job.VideoClipId == video.Id);
        return VideoClipDto.FromEntity(video, _publicFilePath, latestJob);
    }

    [Authorize]
    [HttpPut("{videoId}")]
    public async Task<ActionResult<VideoClipDto>> EditVideo(string videoId, [FromBody] VideoRequest request)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return BadRequest("Unable to get user info");

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            return BadRequest("Name is required and must be 200 characters or fewer");
        if (request.Description.Length > 1000)
            return BadRequest("Description must be 1000 characters or fewer");

        var video = await context.VideoClips
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.VideoId == videoId && v.UserId == userId);

        if (video == null) return NotFound();

        video.Name = request.Name;
        video.Unlisted = request.Unlisted;
        video.Description = request.Description;
        await context.SaveChangesAsync();

        var latestJob = await context.VideoConversionJobs.OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(job => job.VideoClipId == video.Id);
        return VideoClipDto.FromEntity(video, _publicFilePath, latestJob);
    }

    [Authorize]
    [HttpPost("{videoId}/retry")]
    public async Task<ActionResult<VideoClipDto>> RetryVideo(string videoId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return BadRequest("Unable to get user info");

        var video = await context.VideoClips
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.VideoId == videoId && v.UserId == userId);
        if (video == null) return NotFound();

        var latestJob = await context.VideoConversionJobs.OrderByDescending(job => job.CreatedAt)
            .FirstOrDefaultAsync(job => job.VideoClipId == video.Id);
        if (latestJob == null || latestJob.Status != "Error")
            return BadRequest("Only videos with a failed conversion job can be retried");

        latestJob.Status = "Pending";
        latestJob.Progress = 0;
        latestJob.StartedAt = null;
        latestJob.CompletedAt = null;
        await context.SaveChangesAsync();

        return VideoClipDto.FromEntity(video, _publicFilePath, latestJob);
    }

    [Authorize]
    [HttpDelete("{videoId}")]
    public async Task<ActionResult<bool>> DeleteVideo(string videoId)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return BadRequest("Unable to get user info");

        var video = await context.VideoClips
            .FirstOrDefaultAsync(v => v.VideoId == videoId && v.UserId == userId);
        if (video == null) return NotFound();

        // Grab any lingering raw uploads (e.g. from a failed conversion job that was never retried)
        // before the cascade delete removes the job rows.
        var inputPaths = await context.VideoConversionJobs
            .Where(j => j.VideoClipId == video.Id)
            .Select(j => j.InputPath)
            .ToListAsync();

        context.VideoClips.Remove(video);
        await context.SaveChangesAsync();
        try
        {
            foreach (var inputPath in inputPaths)
                if (!string.IsNullOrEmpty(inputPath) && System.IO.File.Exists(inputPath))
                    System.IO.File.Delete(inputPath);

            // Delete source file
            var sourcePath = Path.Combine(_outputVideoFolder,
                video.SourceVideoFile.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (System.IO.File.Exists(sourcePath))
                System.IO.File.Delete(sourcePath);

            // Delete thumbnail file
            var thumbnailPath = Path.Combine(_outputVideoFolder,
                video.Thumbnail.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (System.IO.File.Exists(thumbnailPath))
                System.IO.File.Delete(thumbnailPath);

            var baseHlsFolder =
                Path.GetDirectoryName(video.HlsPlaylistFile.TrimStart(Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrEmpty(baseHlsFolder))
            {
                var hlsFolder = Path.Combine(_outputVideoFolder, baseHlsFolder);
                if (Directory.Exists(hlsFolder))
                    Directory.Delete(hlsFolder, true);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Unable to delete files for video {VideoId}", videoId);
        }

        return true;
    }
}
