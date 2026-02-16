using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClipViewer.Data.Models;

public class VideoConversionJob(int authorId, string inputPath, string outputDirectory, Guid jobId)
{
    public int Id { get; set; }
    
    [Required]
    public Guid VideoClipId { get; set; }

    [ForeignKey("VideoClipId")]
    public VideoClip VideoClip { get; set; } = null!;
    
    public string? VideoId { get; set; }
    public string InputPath { get; } = inputPath;
    public string OutputDirectory { get; } = outputDirectory;
    public Guid JobId { get; } = jobId;
    public int AuthorId { get; set; } = authorId;
    public string? Name { get; set; }

    public int StartTime { get; set; } = 0;
    public int? EndTime { get; set; } = null;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Pending";

    public override string ToString()
    {
        return $"{JobId}: {InputPath}";
    }
}
