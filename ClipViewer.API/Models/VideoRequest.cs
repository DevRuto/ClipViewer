namespace ClipViewer.API.Models;

public class VideoRequest
{
    public string Name { get; set; } = string.Empty;
    public bool Unlisted { get; set; } = false;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
}
