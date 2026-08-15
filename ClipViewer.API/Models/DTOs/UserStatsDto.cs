namespace ClipViewer.API.Models.DTOs;

public class UserStatsDto
{
    public string Username { get; set; } = string.Empty;
    public DateTime MemberSince { get; set; }
    public int TotalClips { get; set; }
    public int ProcessedClips { get; set; }
    public int ProcessingClips { get; set; }
    public int UnlistedClips { get; set; }
    public double TotalDurationSeconds { get; set; }
    public long TotalStorageBytes { get; set; }
    public DateTime? LatestUploadAt { get; set; }
}
