using AITimesheet.API.DTOs;
using AITimesheet.API.Entities;
using AITimesheet.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.API.Controllers;

/// <summary>
/// Handles Microsoft Entra ID (Azure AD) login hand-off.
/// The actual OAuth2/OIDC redirect flow happens on the React frontend via MSAL;
/// this endpoint just upserts the user profile once the frontend has a valid token.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db) => _db = db;

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            user = new User
            {
                Email = request.Email,
                FullName = request.FullName,
                AzureAdObjectId = request.AzureAdObjectId,
                Role = "Employee"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role));
    }

    [HttpGet("me/{userId:guid}")]
    public async Task<ActionResult<UserDto>> Me(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role));
    }
}
