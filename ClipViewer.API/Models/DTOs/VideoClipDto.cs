using ClipViewer.Data.Models;

namespace ClipViewer.API.Models.DTOs;

public class VideoClipDto
{
    public Guid Id { get; set; }

    public string VideoId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;

    public string SourceVideoFile { get; set; } = string.Empty;

    public string HlsPlaylistFile { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }
    public string Author { get; set; } = string.Empty;
    public bool Unlisted { get; set; }
    public int Progress { get; set; }
    public string? Status { get; set; }
    public List<string> Tags { get; set; } = [];

    // Add a static method to map from entity to DTO
    public static VideoClipDto FromEntity(
        VideoClip entity, string publicFilePath = "", VideoConversionJob? latestJob = null)
    {
        return new VideoClipDto
        {
            Id = entity.Id,
            VideoId = entity.VideoId,
            Name = entity.Name,
            Description = entity.Description,
            SourceVideoFile = string.IsNullOrEmpty(entity.SourceVideoFile)
                ? ""
                : $"{publicFilePath}{entity.SourceVideoFile}",
            HlsPlaylistFile = string.IsNullOrEmpty(entity.HlsPlaylistFile)
                ? ""
                : $"{publicFilePath}{entity.HlsPlaylistFile}",
            Thumbnail = string.IsNullOrEmpty(entity.Thumbnail)
                ? ""
                : $"{publicFilePath}{entity.Thumbnail}",
            Duration = entity.Duration,
            CreatedAt = entity.CreatedAt,
            Processed = entity.Processed,
            Author = entity.User?.Username ?? "",
            Unlisted = entity.Unlisted,
            Progress = latestJob?.Progress ?? 0,
            Status = latestJob?.Status ?? "Pending",
            Tags = entity.Tags
        };
    }
}
