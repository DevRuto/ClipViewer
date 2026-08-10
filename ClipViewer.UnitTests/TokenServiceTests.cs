using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClipViewer.API.Models.Auth;
using ClipViewer.API.Services;
using ClipViewer.Data.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClipViewer.UnitTests;

public class TokenServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "this-is-a-super-secret-test-signing-key-1234567890",
        Issuer = "ClipViewerTests",
        Audience = "ClipViewerTestsAudience",
        ExpirationInMinutes = 60
    };

    private static TokenService CreateService(JwtSettings? settings = null)
    {
        return new TokenService(Options.Create(settings ?? Settings));
    }

    private static User CreateUser()
    {
        return new User { Id = 42, Username = "alice", ApiKey = Guid.NewGuid() };
    }

    [Fact]
    public void GenerateToken_ReturnsParsableJwt()
    {
        var service = CreateService();
        var token = service.GenerateToken(CreateUser());

        Assert.False(string.IsNullOrWhiteSpace(token));
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public void GenerateToken_IncludesUserIdAndUsernameClaims()
    {
        var service = CreateService();
        var user = CreateUser();
        var token = service.GenerateToken(user);

        // Validate the same way the JwtBearer middleware would, so claim-type
        // remapping performed by JwtSecurityTokenHandler is accounted for.
        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Settings.Secret))
        }, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(user.Username, principal.FindFirst(ClaimTypes.Name)?.Value);
    }

    [Fact]
    public void GenerateToken_SetsConfiguredIssuerAndAudience()
    {
        var service = CreateService();
        var token = service.GenerateToken(CreateUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Settings.Issuer, jwt.Issuer);
        Assert.Equal(Settings.Audience, jwt.Audiences.First());
    }

    [Fact]
    public void GenerateToken_SetsExpirationBasedOnConfiguration()
    {
        var settings = new JwtSettings
        {
            Secret = Settings.Secret,
            Issuer = Settings.Issuer,
            Audience = Settings.Audience,
            ExpirationInMinutes = 5
        };
        var service = CreateService(settings);
        var before = DateTime.UtcNow;
        var token = service.GenerateToken(CreateUser());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.True(jwt.ValidTo > before.AddMinutes(4));
        Assert.True(jwt.ValidTo <= before.AddMinutes(5).AddSeconds(30));
    }

    [Fact]
    public void GenerateToken_DifferentUsers_ProduceDifferentTokens()
    {
        var service = CreateService();
        var tokenA = service.GenerateToken(new User { Id = 1, Username = "alice", ApiKey = Guid.NewGuid() });
        var tokenB = service.GenerateToken(new User { Id = 2, Username = "bob", ApiKey = Guid.NewGuid() });

        Assert.NotEqual(tokenA, tokenB);
    }
}
