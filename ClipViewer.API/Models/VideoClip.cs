namespace ClipViewer.API.Models;

public class VideoClip
{
    public Guid Id { get; set; }
    public string VideoId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsProcessed { get; set; }
}