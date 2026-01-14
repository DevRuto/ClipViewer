using ClipViewer.API.Data;
using ClipViewer.API.Models;
using ClipViewer.API.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Services;

public interface IAuthService
{
    Task<RegisterResponse> RegisterUserAsync(RegisterRequest request);
    Task<User?> ValidateApiKeyAsync(Guid apiKey);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RegisterResponse> RegisterUserAsync(RegisterRequest request)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            throw new InvalidOperationException("Username already exists");

        // Generate API key
        var apiKey = GenerateApiKey();

        var user = new User
        {
            Username = request.Username.ToLower(),
            ApiKey = apiKey,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new RegisterResponse
        {
            Username = user.Username,
            ApiKey = user.ApiKey
        };
    }

    public async Task<User?> ValidateApiKeyAsync(Guid apiKey)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.ApiKey == apiKey);
    }

    private static Guid GenerateApiKey()
    {
        return Guid.NewGuid();
    }
}
