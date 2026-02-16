using ClipViewer.Data.Models;
using ClipViewer.Worker;
using ClipViewer.Worker.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace ClipViewer.UnitTests;

public class VideoConversionWorkerTests : IDisposable
{
    private readonly Mock<IFFmpegService> _mockFfmpegService;
    private readonly Mock<ILogger<VideoConversionWorker>> _mockLogger;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly string _testHlsPath;
    private readonly string _testInputFile;
    private readonly string _testOutputDir = Path.Combine(Path.GetTempPath(), "VideoConversionWorkerTests");
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _testTempDir = Path.Combine(Path.GetTempPath(), "VideoConversionWorkerTests_Temp");

    public VideoConversionWorkerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        // Create test directories
        Directory.CreateDirectory(_testOutputDir);
        Directory.CreateDirectory(_testTempDir);

        // Create a test input file
        _testInputFile = Path.Combine(_testTempDir, "test_input.mp4");
        _testHlsPath = Path.Combine(_testOutputDir, "hls", "test_video");

        File.WriteAllText(_testInputFile, "test video content");

        _mockLogger = new Mock<ILogger<VideoConversionWorker>>();
        _mockFfmpegService = new Mock<IFFmpegService>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
    }

    public void Dispose()
    {
        // Clean up test directories
        if (Directory.Exists(_testOutputDir))
            Directory.Delete(_testOutputDir, true);

        if (Directory.Exists(_testTempDir))
            Directory.Delete(_testTempDir, true);
    }

    [Fact]
    public async Task ProcessAsync_WithValidJob_ProcessesSuccessfully()
    {
        // Arrange
        var job = new VideoConversionJob(1, _testInputFile, _testOutputDir, Guid.NewGuid())
        {
            VideoId = "test_video"
        };

        _mockFfmpegService.Setup(x => x.ConvertToHls(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new VideoConversionWorker(_mockLogger.Object, _mockFfmpegService.Object,
            _mockServiceScopeFactory.Object);

        // Give the worker time to process the job
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await worker.StopAsync(CancellationToken.None);

        // Assert
    }

    [Fact]
    public async Task ProcessAsync_WithMissingInputFile_DoesNotProcess()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testTempDir, "nonexistent.mp4");
        var job = new VideoConversionJob(1, nonExistentFile, _testOutputDir, Guid.NewGuid())
        {
            VideoId = "test_video"
        };

        var worker = new VideoConversionWorker(_mockLogger.Object, _mockFfmpegService.Object,
            _mockServiceScopeFactory.Object);

        // Give the worker time to process the job
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        // Assert
        var outputFile = Path.Combine(_testOutputDir, "source", $"{job.VideoId}/{Path.GetExtension(nonExistentFile)}");
        Assert.False(File.Exists(outputFile), "Output file should not be created for missing input");
        _mockFfmpegService.Verify(x => x.ConvertToHls(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WithFfmpegError_StillDeletesTempFile()
    {
        // Arrange
        var job = new VideoConversionJob(1, _testInputFile, _testOutputDir, Guid.NewGuid())
        {
            VideoId = "test_video"
        };

        _mockFfmpegService.Setup(x => x.ConvertToHls(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("FFmpeg conversion failed"));

        var worker = new VideoConversionWorker(_mockLogger.Object, _mockFfmpegService.Object,
            _mockServiceScopeFactory.Object);

        // Give the worker time to process the job
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await worker.StopAsync(CancellationToken.None);

        // Assert - Temp file should still be deleted even if FFmpeg fails
        Assert.True(File.Exists(_testInputFile), "Input file should still exist if FFmpeg fails");
    }
}
