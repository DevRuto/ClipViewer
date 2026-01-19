using System.Threading.Channels;
using ClipViewer.API.Data;
using ClipViewer.API.Models;
using ClipViewer.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClipViewer.UnitTests;

public class VideoJobQueueTests
{
    [Fact]
    public async Task EnqueueAsync_AddsJobToQueue()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<VideoConversionJob>();
        var mockLogger = new Mock<ILogger<VideoJobQueue>>();
        var queue = new VideoJobQueue(CreateMockScopeFactory(), channel, mockLogger.Object);
        var job = new VideoConversionJob(1, "input.mp4", "output", Guid.NewGuid());

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
        var queue = new VideoJobQueue(CreateMockScopeFactory(), channel, mockLogger.Object);
        var job1 = new VideoConversionJob(1, "input1.mp4", "output", Guid.NewGuid());
        var job2 = new VideoConversionJob(2, "input2.mp4", "output", Guid.NewGuid());

        // Act
        var result1 = await queue.EnqueueAsync(job1);
        var result2 = await queue.EnqueueAsync(job2);

        // Assert
        Assert.NotEqual(result1, result2);
    }


    private static IServiceScopeFactory CreateMockScopeFactory()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var mockDbContext = new ApplicationDbContext(options);
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(x => x.GetService(typeof(ApplicationDbContext))).Returns(mockDbContext);
        mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(CreateMockScopeFactory);

        // 3. Mock IServiceScope to return the mock service provider
        var mockServiceScope = new Mock<IServiceScope>();
        mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockServiceProvider.Object);

        // 4. Mock IServiceScopeFactory to return the mock service scope
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(mockServiceScope.Object);

        return mockServiceScopeFactory.Object;
    }
}
