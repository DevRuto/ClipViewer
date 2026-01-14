using System.Security.Claims;
using ClipViewer.API.Data;
using ClipViewer.API.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VideosController(
    IConfiguration configuration,
    ApplicationDbContext context) : ControllerBase
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

            // If viewer is NOT logged in, hide unlisted videos
            if (string.IsNullOrEmpty(currentUser))
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
        return VideoClipDto.FromEntity(video, _publicFilePath);
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
        context.VideoClips.Remove(video);
        await context.SaveChangesAsync();
        try
        {
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
            Console.WriteLine("Unable to delete files");
        }

        return true;
    }
}
