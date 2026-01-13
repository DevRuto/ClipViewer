using System.ComponentModel.DataAnnotations;

namespace ClipViewer.API.Models.Auth;

public class RegisterRequest
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public string Username { get; set; } = string.Empty;
    public Guid ApiKey { get; set; } = Guid.Empty;
}

public class LoginRequest
{
    [Required]
    public Guid ApiKey { get; set; } = Guid.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
