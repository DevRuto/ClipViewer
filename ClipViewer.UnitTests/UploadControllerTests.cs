using System.Security.Claims;
using ClipViewer.API.Controllers;
using ClipViewer.API.Interfaces;
using ClipViewer.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ClipViewer.UnitTests;

public class UploadControllerTests : IDisposable
{
    private readonly Mock<IVideoJobQueue> _videoJobQueue = new();
    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "UploadControllerTests_" + Guid.NewGuid());
    private readonly string _outputFolder = Path.Combine(Path.GetTempPath(), "UploadControllerTests_Output_" + Guid.NewGuid());

    public UploadControllerTests()
    {
        Directory.CreateDirectory(_tempFolder);
        Directory.CreateDirectory(_outputFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempFolder)) Directory.Delete(_tempFolder, true);
        if (Directory.Exists(_outputFolder)) Directory.Delete(_outputFolder, true);
    }

    private UploadController CreateController(long? maxUploadSizeBytes = null, int? userId = 1, byte[]? body = null,
        long? contentLength = null)
    {
        var configValues = new Dictionary<string, string?>
        {
            ["UploadOptions:OutputVideoFolder"] = _outputFolder,
            ["UploadOptions:TempVideoFolder"] = _tempFolder
        };
        if (maxUploadSizeBytes.HasValue)
            configValues["UploadOptions:MaxUploadSizeBytes"] = maxUploadSizeBytes.Value.ToString();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        var httpContext = new DefaultHttpContext();
        if (userId.HasValue)
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            ], "Test"));
        else
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        body ??= "fake video bytes"u8.ToArray();
        httpContext.Request.Body = new MemoryStream(body);
        httpContext.Request.ContentLength = contentLength ?? body.Length;

        return new UploadController(configuration, _videoJobQueue.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    [Fact]
    public async Task Upload_WithoutUserClaim_ReturnsBadRequest()
    {
        var controller = CreateController(userId: null);

        var result = await controller.Upload("video/mp4");

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_WithUnsupportedContentType_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.Upload("application/pdf");

        Assert.IsType<BadRequestObjectResult>(result);
        _videoJobQueue.Verify(q => q.EnqueueAsync(It.IsAny<VideoConversionJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Upload_ExceedingMaxUploadSize_ReturnsPayloadTooLarge()
    {
        var controller = CreateController(maxUploadSizeBytes: 10, contentLength: 1000);

        var result = await controller.Upload("video/mp4");

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, statusResult.StatusCode);
    }

    [Fact]
    public async Task Upload_WithValidVideo_EnqueuesJobAndReturnsAccepted()
    {
        _videoJobQueue.Setup(q => q.EnqueueAsync(It.IsAny<VideoConversionJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("generatedVideoId");
        var controller = CreateController();

        var result = await controller.Upload("video/mp4", name: "My Video");

        var accepted = Assert.IsType<AcceptedResult>(result);
        var value = accepted.Value!;
        Assert.Equal("generatedVideoId", value.GetType().GetProperty("videoId")!.GetValue(value));
        Assert.Equal("generatedVideoId.mp4", value.GetType().GetProperty("filename")!.GetValue(value));

        _videoJobQueue.Verify(q => q.EnqueueAsync(
            It.Is<VideoConversionJob>(j => j.AuthorId == 1 && j.Name == "My Video"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Upload_WritesFileToTempFolder()
    {
        _videoJobQueue.Setup(q => q.EnqueueAsync(It.IsAny<VideoConversionJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("videoId123");
        var controller = CreateController(body: "hello video content"u8.ToArray());

        await controller.Upload("video/webm");

        var files = Directory.GetFiles(_tempFolder);
        Assert.Single(files);
        Assert.EndsWith(".webm", files[0]);
        Assert.Equal("hello video content", File.ReadAllText(files[0]));
    }

    [Fact]
    public async Task Upload_WithValidStartAndEndTime_SetsTrimRangeOnJob()
    {
        VideoConversionJob? capturedJob = null;
        _videoJobQueue.Setup(q => q.EnqueueAsync(It.IsAny<VideoConversionJob>(), It.IsAny<CancellationToken>()))
            .Callback<VideoConversionJob, CancellationToken>((job, _) => capturedJob = job)
            .ReturnsAsync("videoId");
        var controller = CreateController();

        await controller.Upload("video/mp4", startTime: 10, endTime: 30);

        Assert.NotNull(capturedJob);
        Assert.Equal(10, capturedJob!.StartTime);
        Assert.Equal(30, capturedJob.EndTime);
    }

    [Fact]
    public async Task Upload_WithEndTimeBeforeStartTime_IgnoresTrimRange()
    {
        VideoConversionJob? capturedJob = null;
        _videoJobQueue.Setup(q => q.EnqueueAsync(It.IsAny<VideoConversionJob>(), It.IsAny<CancellationToken>()))
            .Callback<VideoConversionJob, CancellationToken>((job, _) => capturedJob = job)
            .ReturnsAsync("videoId");
        var controller = CreateController();

        await controller.Upload("video/mp4", startTime: 30, endTime: 10);

        Assert.NotNull(capturedJob);
        Assert.Equal(0, capturedJob!.StartTime);
        Assert.Null(capturedJob.EndTime);
    }
}
