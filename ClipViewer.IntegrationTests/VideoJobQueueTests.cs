using ClipViewer.API.Services;
using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClipViewer.IntegrationTests;

public class VideoJobQueueTests
{
    private static (VideoJobQueue queue, IServiceProvider provider) CreateQueue()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        var logger = new Mock<ILogger<VideoJobQueue>>();
        var queue = new VideoJobQueue(provider.GetRequiredService<IServiceScopeFactory>(), logger.Object);
        return (queue, provider);
    }

    [Fact]
    public async Task EnqueueAsync_AssignsGeneratedVideoId()
    {
        var (queue, _) = CreateQueue();
        var job = new VideoConversionJob(1, "/tmp/input.mp4", "/tmp/output", Guid.NewGuid());

        var videoId = await queue.EnqueueAsync(job);

        Assert.False(string.IsNullOrWhiteSpace(videoId));
        Assert.Equal(videoId, job.VideoId);
        // base64url-safe chars only, no padding
        Assert.DoesNotContain('+', videoId);
        Assert.DoesNotContain('/', videoId);
        Assert.DoesNotContain('=', videoId);
    }

    [Fact]
    public async Task EnqueueAsync_CreatesVideoClipAndJobRows()
    {
        var (queue, provider) = CreateQueue();
        var job = new VideoConversionJob(7, "/tmp/input.mp4", "/tmp/output", Guid.NewGuid());

        var videoId = await queue.EnqueueAsync(job);

        await using var context = provider.GetRequiredService<ApplicationDbContext>();
        var clip = await context.VideoClips.SingleAsync(c => c.VideoId == videoId);
        var savedJob = await context.VideoConversionJobs.SingleAsync(j => j.VideoId == videoId);

        Assert.Equal(7, clip.UserId);
        Assert.Equal(savedJob.VideoClipId, clip.Id);
        Assert.Equal($"/source/{videoId}.mp4", clip.SourceVideoFile);
        Assert.False(clip.Processed);
    }

    [Fact]
    public async Task EnqueueAsync_WithoutName_DefaultsNameToVideoId()
    {
        var (queue, provider) = CreateQueue();
        var job = new VideoConversionJob(1, "/tmp/input.mp4", "/tmp/output", Guid.NewGuid());

        var videoId = await queue.EnqueueAsync(job);

        await using var context = provider.GetRequiredService<ApplicationDbContext>();
        var clip = await context.VideoClips.SingleAsync(c => c.VideoId == videoId);
        Assert.Equal(videoId, clip.Name);
    }

    [Fact]
    public async Task EnqueueAsync_WithName_UsesProvidedName()
    {
        var (queue, provider) = CreateQueue();
        var job = new VideoConversionJob(1, "/tmp/input.mp4", "/tmp/output", Guid.NewGuid())
        {
            Name = "My Clip"
        };

        var videoId = await queue.EnqueueAsync(job);

        await using var context = provider.GetRequiredService<ApplicationDbContext>();
        var clip = await context.VideoClips.SingleAsync(c => c.VideoId == videoId);
        Assert.Equal("My Clip", clip.Name);
    }

    [Fact]
    public async Task EnqueueAsync_MultipleCalls_GenerateUniqueVideoIds()
    {
        var (queue, _) = CreateQueue();

        var id1 = await queue.EnqueueAsync(new VideoConversionJob(1, "/tmp/a.mp4", "/tmp/output", Guid.NewGuid()));
        var id2 = await queue.EnqueueAsync(new VideoConversionJob(1, "/tmp/b.mp4", "/tmp/output", Guid.NewGuid()));

        Assert.NotEqual(id1, id2);
    }
}
