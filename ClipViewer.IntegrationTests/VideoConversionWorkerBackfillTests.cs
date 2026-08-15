using ClipViewer.Data;
using ClipViewer.Data.Models;
using ClipViewer.Worker;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.IntegrationTests;

public class VideoConversionWorkerBackfillTests : IDisposable
{
    private readonly string _outputFolder =
        Path.Combine(Path.GetTempPath(), "BackfillTests_" + Guid.NewGuid());

    private readonly ApplicationDbContext _context;

    public VideoConversionWorkerBackfillTests()
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

    private async Task<VideoClip> SeedClipAsync(bool processed = true, long initialSizeBytes = 0)
    {
        var user = await _context.Users.FindAsync(1);
        if (user == null)
        {
            user = new User { Id = 1, Username = "author", ApiKey = Guid.NewGuid() };
            _context.Users.Add(user);
        }

        var clip = new VideoClip
        {
            Id = Guid.NewGuid(),
            VideoId = "vid",
            Name = "vid",
            SourceVideoFile = "/source/vid.mp4",
            Thumbnail = "/thumbnails/vid.jpg",
            HlsPlaylistFile = "/hls/vid/playlist.m3u8",
            Processed = processed,
            SizeBytes = initialSizeBytes,
            UserId = user.Id,
            User = user
        };
        _context.VideoClips.Add(clip);
        await _context.SaveChangesAsync();
        return clip;
    }

    private void WriteOutputFiles()
    {
        Directory.CreateDirectory(Path.Combine(_outputFolder, "source"));
        Directory.CreateDirectory(Path.Combine(_outputFolder, "thumbnails"));
        Directory.CreateDirectory(Path.Combine(_outputFolder, "hls", "vid"));
        File.WriteAllText(Path.Combine(_outputFolder, "source", "vid.mp4"), "12345"); // 5 bytes
        File.WriteAllText(Path.Combine(_outputFolder, "thumbnails", "vid.jpg"), "123"); // 3 bytes
        File.WriteAllText(Path.Combine(_outputFolder, "hls", "vid", "playlist.m3u8"), "1234567"); // 7 bytes
        File.WriteAllText(Path.Combine(_outputFolder, "hls", "vid", "seg0.ts"), "12"); // 2 bytes
    }

    [Fact]
    public async Task BackfillClipSizesAsync_ComputesAndPersistsSizeFromDisk()
    {
        var clip = await SeedClipAsync();
        WriteOutputFiles();

        var count = await VideoConversionWorker.BackfillClipSizesAsync(_context, _outputFolder);

        Assert.Equal(1, count);
        var reloaded = await _context.VideoClips.FirstAsync(c => c.Id == clip.Id);
        Assert.Equal(17, reloaded.SizeBytes); // 5 + 3 + 7 + 2
    }

    [Fact]
    public async Task BackfillClipSizesAsync_SkipsUnprocessedClips()
    {
        var clip = await SeedClipAsync(processed: false, initialSizeBytes: 42);
        WriteOutputFiles();

        var count = await VideoConversionWorker.BackfillClipSizesAsync(_context, _outputFolder);

        Assert.Equal(0, count);
        var reloaded = await _context.VideoClips.FirstAsync(c => c.Id == clip.Id);
        Assert.Equal(42, reloaded.SizeBytes);
    }

    [Fact]
    public async Task BackfillClipSizesAsync_WithMissingFilesOnDisk_SetsSizeToZero()
    {
        await SeedClipAsync();
        // No files written to _outputFolder

        var count = await VideoConversionWorker.BackfillClipSizesAsync(_context, _outputFolder);

        Assert.Equal(1, count);
        var reloaded = await _context.VideoClips.FirstAsync();
        Assert.Equal(0, reloaded.SizeBytes);
    }

    [Fact]
    public async Task BackfillClipSizesAsync_OverwritesPreviouslyComputedSize()
    {
        var clip = await SeedClipAsync(initialSizeBytes: 999_999);
        WriteOutputFiles();

        await VideoConversionWorker.BackfillClipSizesAsync(_context, _outputFolder);

        var reloaded = await _context.VideoClips.FirstAsync(c => c.Id == clip.Id);
        Assert.Equal(17, reloaded.SizeBytes);
    }
}
