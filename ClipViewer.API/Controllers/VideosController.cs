using ClipViewer.API.Data;
using ClipViewer.API.Models.DTOs;
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
    public async Task<ActionResult<List<VideoClipDto>>> Get()
    {
        var videos = await context.VideoClips
            .Include(v => v.User)
            .ToListAsync();
        return videos.Select(v => VideoClipDto.FromEntity(v, _publicFilePath)).ToList();
    }

    [HttpGet("{videoId}")]
    public async Task<ActionResult<VideoClipDto>> Get(string videoId)
    {
        var video = await context.VideoClips
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.VideoId == videoId);
        if (video == null) return NotFound();
        return VideoClipDto.FromEntity(video, _publicFilePath);
    }
}
