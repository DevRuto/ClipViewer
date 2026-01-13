using System.Threading.Channels;
using ClipViewer.API.Models;
using ClipViewer.API.Services;

namespace ClipViewer.UnitTests;

public class VideoJobQueueTests
{
    [Fact]
    public async Task EnqueueAsync_AddsJobToQueue()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<VideoConversionJob>();
        var mockLogger = new Mock<ILogger<VideoJobQueue>>();
        var queue = new VideoJobQueue(channel, mockLogger.Object);
        var job = new VideoConversionJob("input.mp4", "output", Guid.NewGuid());

        // Act
        var result = await queue.EnqueueAsync(job);

        // Assert
        Assert.NotNull(result);
        Assert.True(await channel.Reader.WaitToReadAsync());
        var dequeuedJob = await channel.Reader.ReadAsync();
        Assert.Equal(job.JobId, dequeuedJob.JobId);
    }

    [Fact]
    public async Task EnqueueAsync_ReturnsUniqueFilenames()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<VideoConversionJob>();
        var mockLogger = new Mock<ILogger<VideoJobQueue>>();
        var queue = new VideoJobQueue(channel, mockLogger.Object);
        var job1 = new VideoConversionJob("input1.mp4", "output", Guid.NewGuid());
        var job2 = new VideoConversionJob("input2.mp4", "output", Guid.NewGuid());

        // Act
        var result1 = await queue.EnqueueAsync(job1);
        var result2 = await queue.EnqueueAsync(job2);

        // Assert
        Assert.NotEqual(result1, result2);
    }
}
