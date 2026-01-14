namespace ClipViewer.API.Models.DTOs;

public class VideoClipDto
{
    public Guid Id { get; set; }

    public string VideoId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string SourceVideoFile { get; set; } = string.Empty;

    public string HlsPlaylistFile { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }
    public string Author { get; set; } = string.Empty;
    public bool Unlisted { get; set; }

    // Add a static method to map from entity to DTO
    public static VideoClipDto FromEntity(VideoClip entity, string publicFilePath = "")
    {
        return new VideoClipDto
        {
            Id = entity.Id,
            VideoId = entity.VideoId,
            Name = entity.Name,
            SourceVideoFile = $"{publicFilePath}{entity.SourceVideoFile}",
            HlsPlaylistFile = $"{publicFilePath}{entity.HlsPlaylistFile}",
            Thumbnail = $"{publicFilePath}{entity.Thumbnail}",
            Duration = entity.Duration,
            CreatedAt = entity.CreatedAt,
            Processed = entity.Processed,
            Author = entity.User.Username,
            Unlisted = entity.Unlisted
        };
    }
}
