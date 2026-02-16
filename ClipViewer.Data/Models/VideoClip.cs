using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClipViewer.Data.Models;

public class VideoClip
{
    public Guid Id { get; set; }

    [Required]
    public string VideoId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string SourceVideoFile { get; set; } = string.Empty;

    public string HlsPlaylistFile { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool Processed { get; set; }
    public bool Unlisted { get; set; }

    // User relationship
    [Required]
    public int UserId { get; set; }

    [ForeignKey("UserId")]
    public User User { get; set; } = null!;
}
