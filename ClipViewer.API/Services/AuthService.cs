using ClipViewer.API.Data;
using ClipViewer.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Services;

public interface IAuthService
{
    Task<User?> ValidateApiKeyAsync(Guid apiKey);
}

public class AuthService(ApplicationDbContext context) : IAuthService
{
    public async Task<User?> ValidateApiKeyAsync(Guid apiKey)
    {
        return await context.Users
            .FirstOrDefaultAsync(u => u.ApiKey == apiKey);
    }
}
