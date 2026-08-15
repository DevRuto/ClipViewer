using System.Security.Claims;
using ClipViewer.API.Controllers;
using ClipViewer.API.Models.DTOs;
using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.IntegrationTests;

public class StatsControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public StatsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private StatsController CreateController(int? userId = null)
    {
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Name, $"user{userId}")
            ], "Test"));
        else
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        return new StatsController(_context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private async Task<User> SeedUserAsync(int userId, DateTime? createdAt = null)
    {
        var user = new User
        {
            Id = userId, Username = $"user{userId}", ApiKey = Guid.NewGuid(),
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<VideoClip> SeedVideoAsync(int userId, string name, bool processed = true,
        bool unlisted = false, TimeSpan? duration = null, DateTime? createdAt = null, long sizeBytes = 0)
    {
        var clip = new VideoClip
        {
            Id = Guid.NewGuid(),
            VideoId = Guid.NewGuid().ToString("N")[..8],
            Name = name,
            SourceVideoFile = "/source/video.mp4",
            Processed = processed,
            Unlisted = unlisted,
            Duration = duration ?? TimeSpan.Zero,
            SizeBytes = sizeBytes,
            UserId = userId,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        _context.VideoClips.Add(clip);
        await _context.SaveChangesAsync();
        return clip;
    }

    [Fact]
    public async Task GetMyStats_WithoutUserClaim_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetMyStats();

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetMyStats_WithUnknownUser_ReturnsNotFound()
    {
        var controller = CreateController(userId: 1);

        var result = await controller.GetMyStats();

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMyStats_AggregatesOnlyOwnClips()
    {
        var memberSince = DateTime.UtcNow.AddDays(-30);
        await SeedUserAsync(1, memberSince);
        await SeedUserAsync(2);

        await SeedVideoAsync(1, "Processed Public", processed: true, unlisted: false,
            duration: TimeSpan.FromSeconds(30));
        await SeedVideoAsync(1, "Still Processing", processed: false, unlisted: false,
            duration: TimeSpan.Zero);
        await SeedVideoAsync(1, "Unlisted", processed: true, unlisted: true,
            duration: TimeSpan.FromSeconds(90));
        await SeedVideoAsync(2, "Someone Else's Video", processed: true);

        var controller = CreateController(userId: 1);

        var result = await controller.GetMyStats();

        var stats = Assert.IsType<UserStatsDto>(result.Value);
        Assert.Equal("user1", stats.Username);
        Assert.Equal(memberSince, stats.MemberSince);
        Assert.Equal(3, stats.TotalClips);
        Assert.Equal(2, stats.ProcessedClips);
        Assert.Equal(1, stats.ProcessingClips);
        Assert.Equal(1, stats.UnlistedClips);
        Assert.Equal(120, stats.TotalDurationSeconds);
    }

    [Fact]
    public async Task GetMyStats_WithNoClips_ReturnsZeroedStatsAndNullLatestUpload()
    {
        await SeedUserAsync(1);
        var controller = CreateController(userId: 1);

        var result = await controller.GetMyStats();

        var stats = Assert.IsType<UserStatsDto>(result.Value);
        Assert.Equal(0, stats.TotalClips);
        Assert.Null(stats.LatestUploadAt);
        Assert.Equal(0, stats.TotalStorageBytes);
    }

    [Fact]
    public async Task GetMyStats_SumsPersistedClipSizes()
    {
        await SeedUserAsync(1);
        await SeedUserAsync(2);
        await SeedVideoAsync(1, "First", sizeBytes: 1000);
        await SeedVideoAsync(1, "Second", sizeBytes: 2500);
        await SeedVideoAsync(2, "Someone Else's Video", sizeBytes: 999_999);

        var controller = CreateController(userId: 1);

        var result = await controller.GetMyStats();

        var stats = Assert.IsType<UserStatsDto>(result.Value);
        Assert.Equal(3500, stats.TotalStorageBytes);
    }

    [Fact]
    public async Task GetMyStats_ReturnsLatestUploadDate()
    {
        await SeedUserAsync(1);
        await SeedVideoAsync(1, "Older", createdAt: DateTime.UtcNow.AddDays(-2));
        var newer = await SeedVideoAsync(1, "Newer", createdAt: DateTime.UtcNow);

        var controller = CreateController(userId: 1);

        var result = await controller.GetMyStats();

        var stats = Assert.IsType<UserStatsDto>(result.Value);
        Assert.Equal(newer.CreatedAt, stats.LatestUploadAt);
    }
}
