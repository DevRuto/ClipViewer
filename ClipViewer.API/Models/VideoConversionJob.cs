namespace ClipViewer.API.Models;

public record VideoConversionJob(string InputPath, string OutputDirectory, Guid JobId);