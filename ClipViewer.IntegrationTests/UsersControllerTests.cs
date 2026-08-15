using ClipViewer.API.Controllers;
using ClipViewer.API.Models.DTOs;
using ClipViewer.API.Models.Users;
using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.IntegrationTests;

public class UsersControllerTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public UsersControllerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private UsersController CreateController() => new(_context);

    private async Task<User> SeedUserAsync(string username, UserRole role = UserRole.User)
    {
        var user = new User { Username = username, ApiKey = Guid.NewGuid(), Role = role };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetUsers_ReturnsAllUsers_WithoutApiKeys()
    {
        await SeedUserAsync("alice", UserRole.Admin);
        await SeedUserAsync("bob");
        var controller = CreateController();

        var result = await controller.GetUsers();

        var users = Assert.IsType<List<UserDto>>(result.Value);
        Assert.Equal(2, users.Count);
        Assert.All(users, u => Assert.Null(u.ApiKey));
        Assert.Contains(users, u => u is { Username: "alice", Role: nameof(UserRole.Admin) });
        Assert.Contains(users, u => u is { Username: "bob", Role: nameof(UserRole.User) });
    }

    [Fact]
    public async Task CreateUser_WithValidUsername_CreatesUserAndReturnsApiKey()
    {
        var controller = CreateController();

        var result = await controller.CreateUser(new CreateUserRequest { Username = "charlie" });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<UserDto>(created.Value);
        Assert.Equal("charlie", dto.Username);
        Assert.Equal(nameof(UserRole.User), dto.Role);
        Assert.NotNull(dto.ApiKey);
        Assert.NotEqual(Guid.Empty, dto.ApiKey);

        var stored = await _context.Users.SingleAsync(u => u.Username == "charlie");
        Assert.Equal(dto.ApiKey, stored.ApiKey);
        Assert.Equal(UserRole.User, stored.Role);
    }

    [Fact]
    public async Task CreateUser_WithIsAdmin_CreatesAdminUser()
    {
        var controller = CreateController();

        var result = await controller.CreateUser(new CreateUserRequest { Username = "admin-dave", IsAdmin = true });

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var dto = Assert.IsType<UserDto>(created.Value);
        Assert.Equal(nameof(UserRole.Admin), dto.Role);
    }

    [Fact]
    public async Task CreateUser_WithBlankUsername_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.CreateUser(new CreateUserRequest { Username = "   " });

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ReturnsConflict()
    {
        await SeedUserAsync("erin");
        var controller = CreateController();

        var result = await controller.CreateUser(new CreateUserRequest { Username = "erin" });

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task RotateKey_ForExistingUser_GeneratesNewApiKey()
    {
        var user = await SeedUserAsync("frank");
        var originalKey = user.ApiKey;
        var controller = CreateController();

        var result = await controller.RotateKey(user.Id);

        var dto = Assert.IsType<UserDto>(result.Value);
        Assert.NotNull(dto.ApiKey);
        Assert.NotEqual(originalKey, dto.ApiKey);

        var stored = await _context.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal(dto.ApiKey, stored.ApiKey);
    }

    [Fact]
    public async Task RotateKey_ForUnknownUser_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.RotateKey(999);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
