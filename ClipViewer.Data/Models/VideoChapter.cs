using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClipViewer.Data.Models;

public class VideoChapter
{
    public int Id { get; set; }

    [Required]
    public Guid VideoClipId { get; set; }

    [ForeignKey("VideoClipId")]
    public VideoClip VideoClip { get; set; } = null!;

    /// <summary>Chapter start time, in seconds from the start of the video.</summary>
    public int StartTime { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
}
