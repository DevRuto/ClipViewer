using ClipViewer.Data.Models;

namespace ClipViewer.API.Models.DTOs;

public class ChapterDto
{
    public int Id { get; set; }
    public int StartTime { get; set; }
    public string Title { get; set; } = string.Empty;

    public static ChapterDto FromEntity(VideoChapter entity)
    {
        return new ChapterDto
        {
            Id = entity.Id,
            StartTime = entity.StartTime,
            Title = entity.Title
        };
    }
}
