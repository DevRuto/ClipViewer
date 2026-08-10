using System.Security.Claims;
using ClipViewer.API.Controllers;
using ClipViewer.API.Models;
using ClipViewer.API.Models.DTOs;
using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ClipViewer.IntegrationTests;

public class VideosControllerTests : IDisposable
{
    private readonly string _outputFolder =
        Path.Combine(Path.GetTempPath(), "VideosControllerTests_" + Guid.NewGuid());

    private readonly ApplicationDbContext _context;

    public VideosControllerTests()
    {
        Directory.CreateDirectory(_outputFolder);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_outputFolder)) Directory.Delete(_outputFolder, true);
    }

    private VideosController CreateController(int? userId = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["UploadOptions:OutputVideoFolder"] = _outputFolder,
            ["UploadOptions:PublicFilePath"] = "/files"
        }).Build();

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Name, $"user{userId}")
            ], "Test"));
        else
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var logger = new Mock<ILogger<VideosController>>();
        return new VideosController(configuration, _context, logger.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private async Task<(User user, VideoClip clip)> SeedVideoAsync(int userId, string name, bool unlisted = false,
        DateTime? createdAt = null)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            user = new User { Id = userId, Username = $"user{userId}", ApiKey = Guid.NewGuid() };
            _context.Users.Add(user);
        }

        var clip = new VideoClip
        {
            Id = Guid.NewGuid(),
            VideoId = Guid.NewGuid().ToString("N")[..8],
            Name = name,
            SourceVideoFile = "/source/video.mp4",
            Unlisted = unlisted,
            UserId = userId,
            User = user,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
        _context.VideoClips.Add(clip);
        await _context.SaveChangesAsync();
        return (user, clip);
    }

    [Fact]
    public async Task GetVideoList_WithoutUserFilter_ExcludesUnlistedVideos()
    {
        await SeedVideoAsync(1, "Public Video", unlisted: false);
        await SeedVideoAsync(1, "Unlisted Video", unlisted: true);
        var controller = CreateController();

        var result = await controller.GetVideoList();

        var videos = Assert.IsAssignableFrom<List<VideoClipDto>>(result.Value);
        Assert.Single(videos);
        Assert.Equal("Public Video", videos[0].Name);
    }

    [Fact]
    public async Task GetVideoList_OrdersByCreatedAtDescending()
    {
        await SeedVideoAsync(1, "Older", createdAt: DateTime.UtcNow.AddDays(-1));
        await SeedVideoAsync(1, "Newer", createdAt: DateTime.UtcNow);
        var controller = CreateController();

        var result = await controller.GetVideoList();

        var videos = Assert.IsAssignableFrom<List<VideoClipDto>>(result.Value);
        Assert.Equal("Newer", videos[0].Name);
        Assert.Equal("Older", videos[1].Name);
    }

    [Fact]
    public async Task GetVideo_WithExistingId_ReturnsDto()
    {
        var (_, clip) = await SeedVideoAsync(1, "My Video");
        var controller = CreateController();

        var result = await controller.GetVideo(clip.VideoId);

        var dto = Assert.IsType<VideoClipDto>(result.Value);
        Assert.Equal("My Video", dto.Name);
    }

    [Fact]
    public async Task GetVideo_WithUnknownId_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.GetVideo("does-not-exist");

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetVideo_IncludesLatestJobStatusAndProgress()
    {
        var (_, clip) = await SeedVideoAsync(1, "My Video");
        _context.VideoConversionJobs.Add(new VideoConversionJob(1, "/tmp/in.mp4", "/tmp/out", Guid.NewGuid())
        {
            VideoClipId = clip.Id,
            Status = "Processing",
            Progress = 42,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        var controller = CreateController();

        var result = await controller.GetVideo(clip.VideoId);

        var dto = Assert.IsType<VideoClipDto>(result.Value);
        Assert.Equal("Processing", dto.Status);
        Assert.Equal(42, dto.Progress);
    }

    [Fact]
    public async Task EditVideo_AsOwner_UpdatesFields()
    {
        var (_, clip) = await SeedVideoAsync(1, "Old Name");
        var controller = CreateController(userId: 1);

        var result = await controller.EditVideo(clip.VideoId,
            new VideoRequest { Name = "New Name", Unlisted = true, Description = "desc" });

        var dto = Assert.IsType<VideoClipDto>(result.Value);
        Assert.Equal("New Name", dto.Name);
        Assert.True(dto.Unlisted);
        Assert.Equal("desc", dto.Description);
    }

    [Fact]
    public async Task EditVideo_AsNonOwner_ReturnsNotFound()
    {
        var (_, clip) = await SeedVideoAsync(1, "Old Name");
        var controller = CreateController(userId: 2);

        var result = await controller.EditVideo(clip.VideoId,
            new VideoRequest { Name = "New Name" });

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task EditVideo_WithoutUserClaim_ReturnsBadRequest()
    {
        var (_, clip) = await SeedVideoAsync(1, "Old Name");
        var controller = CreateController();

        var result = await controller.EditVideo(clip.VideoId, new VideoRequest { Name = "New Name" });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task DeleteVideo_AsOwner_RemovesRowAndFiles()
    {
        var (_, clip) = await SeedVideoAsync(1, "My Video");
        clip.SourceVideoFile = "/source/vid.mp4";
        clip.Thumbnail = "/thumbnails/vid.jpg";
        clip.HlsPlaylistFile = "/hls/vid/playlist.m3u8";
        await _context.SaveChangesAsync();

        Directory.CreateDirectory(Path.Combine(_outputFolder, "source"));
        Directory.CreateDirectory(Path.Combine(_outputFolder, "thumbnails"));
        Directory.CreateDirectory(Path.Combine(_outputFolder, "hls", "vid"));
        File.WriteAllText(Path.Combine(_outputFolder, "source", "vid.mp4"), "video");
        File.WriteAllText(Path.Combine(_outputFolder, "thumbnails", "vid.jpg"), "thumb");
        File.WriteAllText(Path.Combine(_outputFolder, "hls", "vid", "playlist.m3u8"), "playlist");

        var controller = CreateController(userId: 1);

        var result = await controller.DeleteVideo(clip.VideoId);

        Assert.True(Assert.IsType<bool>(result.Value));
        Assert.False(await _context.VideoClips.AnyAsync(v => v.VideoId == clip.VideoId));
        Assert.False(File.Exists(Path.Combine(_outputFolder, "source", "vid.mp4")));
        Assert.False(File.Exists(Path.Combine(_outputFolder, "thumbnails", "vid.jpg")));
        Assert.False(Directory.Exists(Path.Combine(_outputFolder, "hls", "vid")));
    }

    [Fact]
    public async Task DeleteVideo_AsNonOwner_ReturnsNotFoundAndKeepsRow()
    {
        var (_, clip) = await SeedVideoAsync(1, "My Video");
        var controller = CreateController(userId: 2);

        var result = await controller.DeleteVideo(clip.VideoId);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.True(await _context.VideoClips.AnyAsync(v => v.VideoId == clip.VideoId));
    }

    [Fact]
    public async Task DeleteVideo_WithoutUserClaim_ReturnsBadRequest()
    {
        var (_, clip) = await SeedVideoAsync(1, "My Video");
        var controller = CreateController();

        var result = await controller.DeleteVideo(clip.VideoId);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
