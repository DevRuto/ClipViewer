using System.ComponentModel.DataAnnotations;

namespace ClipViewer.API.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public Guid ApiKey { get; set; } = Guid.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<VideoClip> VideoClips { get; set; } = new List<VideoClip>();
}
