using System.Security.Claims;
using ClipViewer.API.Controllers;
using ClipViewer.API.Interfaces;
using ClipViewer.API.Models.Auth;
using ClipViewer.API.Services;
using ClipViewer.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClipViewer.UnitTests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<ITokenService> _tokenService = new();

    private AuthController CreateController(ClaimsPrincipal? user = null)
    {
        var controller = new AuthController(_authService.Object, _tokenService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task Login_WithInvalidGuidFormat_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = await controller.Login(new LoginRequest { ApiKey = "not-a-guid" });

        Assert.IsType<UnauthorizedResult>(result);
        _authService.Verify(s => s.ValidateApiKeyAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithUnknownApiKey_ReturnsUnauthorized()
    {
        var apiKey = Guid.NewGuid();
        _authService.Setup(s => s.ValidateApiKeyAsync(apiKey)).ReturnsAsync((User?)null);
        var controller = CreateController();

        var result = await controller.Login(new LoginRequest { ApiKey = apiKey.ToString() });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Login_WithValidApiKey_ReturnsTokenFromTokenService()
    {
        var apiKey = Guid.NewGuid();
        var user = new User { Id = 1, Username = "alice", ApiKey = apiKey };
        _authService.Setup(s => s.ValidateApiKeyAsync(apiKey)).ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateToken(user)).Returns("fake-jwt-token");
        var controller = CreateController();

        var result = await controller.Login(new LoginRequest { ApiKey = apiKey.ToString() });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value!;
        var tokenProperty = value.GetType().GetProperty("Token");
        Assert.Equal("fake-jwt-token", tokenProperty!.GetValue(value));
    }

    [Fact]
    public void GetCurrentUser_WithValidClaims_ReturnsUserIdAndUsername()
    {
        var claims = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Name, "alice")
        ], "TestAuth");
        var controller = CreateController(new ClaimsPrincipal(claims));

        var result = controller.GetCurrentUser();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value!;
        Assert.Equal("42", value.GetType().GetProperty("UserId")!.GetValue(value));
        Assert.Equal("alice", value.GetType().GetProperty("Username")!.GetValue(value));
    }

    [Fact]
    public void GetCurrentUser_WithoutClaims_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = controller.GetCurrentUser();

        Assert.IsType<UnauthorizedResult>(result);
    }
}
