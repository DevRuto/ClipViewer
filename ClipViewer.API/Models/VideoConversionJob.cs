namespace ClipViewer.API.Models;

public class VideoConversionJob(int authorId, string inputPath, string outputDirectory, Guid jobId)
{
    public string? VideoId { get; set; }
    public string InputPath { get; } = inputPath;
    public string OutputDirectory { get; } = outputDirectory;
    public Guid JobId { get; } = jobId;
    public int AuthorId { get; set; } = authorId;
    public string? Name { get; set; }

    public override string ToString()
    {
        return $"{JobId}: {InputPath}";
    }
}
