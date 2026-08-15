namespace ClipViewer.API.Models.Users;

public class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}
