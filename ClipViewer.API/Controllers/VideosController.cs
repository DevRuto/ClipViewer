using ClipViewer.API.Data;
using ClipViewer.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VideosController(
    IConfiguration configuration,
    ApplicationDbContext context) : ControllerBase
{
    private readonly string _publicFilePath =
        configuration.GetSection("UploadOptions").GetSection("PublicFilePath").Value ?? "/files";

    [HttpGet]
    public async Task<ActionResult<List<VideoClip>>> Get()
    {
        var videos = await context.VideoClips.ToListAsync();
        foreach (var video in videos)
        {
            video.SourceVideoFile = $"{_publicFilePath}{video.SourceVideoFile}";
            video.Thumbnail = $"{_publicFilePath}{video.Thumbnail}";
            video.HlsPlaylistFile = $"{_publicFilePath}{video.HlsPlaylistFile}";
        }

        return videos;
    }

    [HttpGet("{videoId}")]
    public async Task<ActionResult<VideoClip>> Get(string videoId)
    {
        var video = await context.VideoClips.FirstOrDefaultAsync(v => v.VideoId == videoId);
        if (video == null) return NotFound();
        video.SourceVideoFile = $"{_publicFilePath}{video.SourceVideoFile}";
        video.Thumbnail = $"{_publicFilePath}{video.Thumbnail}";
        video.HlsPlaylistFile = $"{_publicFilePath}{video.HlsPlaylistFile}";
        return video;
    }
}
