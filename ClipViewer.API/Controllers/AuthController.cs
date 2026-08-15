using System.Security.Claims;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models.Auth;
using ClipViewer.API.Services;
using ClipViewer.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClipViewer.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ITokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!Guid.TryParse(request.ApiKey, out var apiKey))
            return Unauthorized();
        var user = await authService.ValidateApiKeyAsync(apiKey);
        if (user == null)
            return Unauthorized();

        var token = tokenService.GenerateToken(user);
        return Ok(new { Token = token });
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "JwtOrApiKey")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? nameof(UserRole.User);

        if (userId == null || username == null)
            return Unauthorized();

        return Ok(new { UserId = userId, Username = username, Role = role });
    }
}
