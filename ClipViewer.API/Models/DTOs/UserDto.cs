using ClipViewer.Data.Models;

namespace ClipViewer.API.Models.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Only populated once, in the response to creating a user or rotating its key -
    // the API key is never returned by the list/get endpoints.
    public Guid? ApiKey { get; set; }

    public static UserDto FromEntity(User user, Guid? apiKey = null)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            ApiKey = apiKey
        };
    }
}
