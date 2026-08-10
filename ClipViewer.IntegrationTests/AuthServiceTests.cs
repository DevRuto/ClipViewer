using ClipViewer.API.Services;
using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.IntegrationTests;

public class AuthServiceTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithMatchingKey_ReturnsUser()
    {
        await using var context = CreateContext();
        var apiKey = Guid.NewGuid();
        var user = new User { Id = 1, Username = "alice", ApiKey = apiKey };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthService(context);
        var result = await service.ValidateApiKeyAsync(apiKey);

        Assert.NotNull(result);
        Assert.Equal("alice", result.Username);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithUnknownKey_ReturnsNull()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { Id = 1, Username = "alice", ApiKey = Guid.NewGuid() });
        await context.SaveChangesAsync();

        var service = new AuthService(context);
        var result = await service.ValidateApiKeyAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithEmptyDatabase_ReturnsNull()
    {
        await using var context = CreateContext();
        var service = new AuthService(context);

        var result = await service.ValidateApiKeyAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
