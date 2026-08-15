using ClipViewer.API.Models.DTOs;
using ClipViewer.API.Models.Users;
using ClipViewer.Data;
using ClipViewer.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClipViewer.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class UsersController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await context.Users.OrderBy(u => u.Username).ToListAsync();
        return users.Select(u => UserDto.FromEntity(u)).ToList();
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var username = request.Username.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
            return BadRequest("Username is required and must be 50 characters or fewer");

        if (await context.Users.AnyAsync(u => u.Username == username))
            return Conflict("A user with that username already exists");

        var user = new User
        {
            Username = username,
            ApiKey = Guid.NewGuid(),
            Role = request.IsAdmin ? UserRole.Admin : UserRole.User,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsers), UserDto.FromEntity(user, user.ApiKey));
    }

    [HttpPost("{id:int}/rotate-key")]
    public async Task<ActionResult<UserDto>> RotateKey(int id)
    {
        var user = await context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.ApiKey = Guid.NewGuid();
        await context.SaveChangesAsync();

        return UserDto.FromEntity(user, user.ApiKey);
    }
}
