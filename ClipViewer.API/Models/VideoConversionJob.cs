namespace ClipViewer.API.Models;

public class VideoConversionJob(string inputPath, string outputDirectory, Guid jobId)
{
    public string? VideoId { get; set; }
    public string InputPath { get; } = inputPath;
    public string OutputDirectory { get; } = outputDirectory;
    public Guid JobId { get; } = jobId;

    public override string ToString()
    {
        return $"{JobId}: {InputPath}";
    }
}
