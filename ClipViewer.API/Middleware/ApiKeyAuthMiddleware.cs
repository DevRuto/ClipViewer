using System.Security.Claims;
using System.Text.Encodings.Web;
using ClipViewer.API.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ClipViewer.API.Middleware;

public class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApplicationDbContext dbContext)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string API_KEY_HEADER = "X-API-Key";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Skip authentication for certain paths
        if (!Request.Path.StartsWithSegments("/api"))
            return AuthenticateResult.NoResult();
        if (!Request.Headers.TryGetValue(API_KEY_HEADER, out var apiKeyValues))
            return AuthenticateResult.Fail("API Key is missing");

        if (!Guid.TryParse(apiKeyValues.FirstOrDefault(), out var apiKey))
            return AuthenticateResult.Fail("API Key is invalid");
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.ApiKey == apiKey);
        if (user == null) return AuthenticateResult.Fail("Invalid API Key");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}

public static class ApiKeyAuthExtensions
{
    public static AuthenticationBuilder AddApiKeyAuth(
        this AuthenticationBuilder builder,
        string authenticationScheme = "ApiKey")
    {
        return builder.AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(
            authenticationScheme, null);
    }
}
