using AITimesheet.IdentityService.DTOs;
using AITimesheet.IdentityService.ServiceLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AITimesheet.IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        return Ok(result);
    }

    [HttpGet("me/{userId:guid}")]
    public async Task<ActionResult<UserDto>> Me(Guid userId, CancellationToken ct)
    {
        var user = await _authService.GetUserByIdAsync(userId, ct);
        if (user is null) return NotFound();
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role, user.ManagerId));
    }

    [HttpPost("logout")]
    public async Task<ActionResult> Logout(CancellationToken ct)
    {
        await _authService.LogoutAsync(ct);
        return Ok();
    }

    // ---- Internal endpoints for service-to-service communication ----

    [HttpGet("internal/users/{userId:guid}")]
    public async Task<ActionResult<UserDto>> GetUserInternal(Guid userId, CancellationToken ct)
    {
        var user = await _authService.GetUserByIdAsync(userId, ct);
        if (user is null) return NotFound();
        return Ok(new UserDto(user.Id, user.FullName, user.Email, user.Role, user.ManagerId));
    }

    [HttpGet("internal/users/manager/{managerId:guid}")]
    public async Task<ActionResult<List<UserDto>>> GetUsersByManagerInternal(Guid managerId, CancellationToken ct)
    {
        var users = await _authService.GetUsersByManagerIdAsync(managerId, ct);
        var dtos = users.Select(u => new UserDto(u.Id, u.FullName, u.Email, u.Role, u.ManagerId)).ToList();
        return Ok(dtos);
    }
}
