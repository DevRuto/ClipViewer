using ClipViewer.Data;
using ClipViewer.Data.Models;
using ClipViewer.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClipViewer.IntegrationTests;

public class VideoConversionWorkerTests : IDisposable
{
    private readonly Mock<ILogger<VideoConversionWorker>> _mockLogger = new();
    private readonly IServiceProvider _provider;
    private readonly string _testInputFile;
    private readonly string _testOutputDir = Path.Combine(Path.GetTempPath(), "VideoConversionWorkerTests_" + Guid.NewGuid());
    private readonly string _testTempDir;

    public VideoConversionWorkerTests()
    {
        _testTempDir = Path.Combine(_testOutputDir, "temp");
        Directory.CreateDirectory(_testOutputDir);
        Directory.CreateDirectory(_testTempDir);

        _testInputFile = Path.Combine(_testTempDir, "test_input.mp4");
        File.WriteAllText(_testInputFile, "not a real video file");

        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        _provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testOutputDir))
            Directory.Delete(_testOutputDir, true);
    }

    private VideoConversionWorker CreateWorker()
    {
        return new VideoConversionWorker(_mockLogger.Object, _provider.GetRequiredService<IServiceScopeFactory>());
    }

    private async Task<VideoConversionJob> SeedJobAsync(string inputPath, string videoId = "test_video")
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = await db.Users.FindAsync(1);
        if (user == null)
        {
            user = new User { Id = 1, Username = "author", ApiKey = Guid.NewGuid() };
            db.Users.Add(user);
        }

        var clip = new VideoClip
        {
            Id = Guid.NewGuid(),
            VideoId = videoId,
            Name = videoId,
            SourceVideoFile = string.Empty,
            UserId = user.Id,
            User = user
        };
        db.VideoClips.Add(clip);

        var job = new VideoConversionJob(user.Id, inputPath, _testOutputDir, Guid.NewGuid())
        {
            VideoId = videoId,
            VideoClipId = clip.Id
        };
        db.VideoConversionJobs.Add(job);

        await db.SaveChangesAsync();
        return job;
    }

    private async Task<VideoConversionJob> ReloadJobAsync(Guid jobId)
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.VideoConversionJobs.FirstAsync(j => j.JobId == jobId);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPendingJobs_DoesNotThrow()
    {
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(300);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingInputFile_LeavesJobProcessingWithoutCreatingOutput()
    {
        var missingInput = Path.Combine(_testTempDir, "does_not_exist.mp4");
        var job = await SeedJobAsync(missingInput);
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(1500);
        await worker.StopAsync(CancellationToken.None);

        var reloaded = await ReloadJobAsync(job.JobId);
        Assert.Equal("Processing", reloaded.Status);

        var sourceFile = Path.Combine(_testOutputDir, "source", $"test_video{Path.GetExtension(missingInput)}");
        Assert.False(File.Exists(sourceFile), "No output should be created for a missing input file");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnprocessableVideo_MarksJobAsErrorAndKeepsInputFile()
    {
        var job = await SeedJobAsync(_testInputFile);
        var worker = CreateWorker();

        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(2000);
        await worker.StopAsync(CancellationToken.None);

        var reloaded = await ReloadJobAsync(job.JobId);
        Assert.Equal("Error", reloaded.Status);
        Assert.True(File.Exists(_testInputFile), "Input file should not be deleted when conversion fails");
    }

    [Fact]
    public async Task ExecuteAsync_PicksUpOldestPendingJobFirst()
    {
        var olderJob = await SeedJobAsync(Path.Combine(_testTempDir, "missing_old.mp4"), "old_video");
        await Task.Delay(10);
        await SeedJobAsync(Path.Combine(_testTempDir, "missing_new.mp4"), "new_video");

        var worker = CreateWorker();
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(1500);
        await worker.StopAsync(CancellationToken.None);

        var reloadedOld = await ReloadJobAsync(olderJob.JobId);
        Assert.Equal("Processing", reloadedOld.Status);
    }
}
