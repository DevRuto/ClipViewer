using System.Security.Claims;
using ClipViewer.API.Models.DTOs;
using ClipViewer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StatsController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserStatsDto>> GetMyStats()
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return BadRequest("Unable to get user info");

        var user = await context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var clips = await context.VideoClips
            .Where(v => v.UserId == userId)
            .ToListAsync();

        return new UserStatsDto
        {
            Username = user.Username,
            MemberSince = user.CreatedAt,
            TotalClips = clips.Count,
            ProcessedClips = clips.Count(c => c.Processed),
            ProcessingClips = clips.Count(c => !c.Processed),
            UnlistedClips = clips.Count(c => c.Unlisted),
            TotalDurationSeconds = clips.Sum(c => c.Duration.TotalSeconds),
            TotalStorageBytes = clips.Sum(c => c.SizeBytes),
            LatestUploadAt = clips.Count > 0 ? clips.Max(c => c.CreatedAt) : null
        };
    }
}
