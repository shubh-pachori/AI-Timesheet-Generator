using AITimesheet.TimesheetService.DTOs;
using AITimesheet.TimesheetService.Entities;
using AITimesheet.TimesheetService.RepositoryLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITimesheet.TimesheetService.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
public class IntegrationController : ControllerBase
{
    private readonly IConnectionRepository _connectionRepo;

    public IntegrationController(IConnectionRepository connectionRepo)
    {
        _connectionRepo = connectionRepo;
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectAccountRequest request)
    {
        if (!Enum.TryParse<ConnectionProvider>(request.Provider, true, out var provider))
            return BadRequest($"Unknown provider '{request.Provider}'");

        var existing = await _connectionRepo.GetByUserAndProviderAsync(request.UserId, provider);

        if (existing is not null)
        {
            existing.AccessToken = request.AccessToken;
            existing.RefreshToken = request.RefreshToken;
            existing.ExternalAccountId = request.ExternalAccountId;
            existing.IsActive = true;
            await _connectionRepo.UpdateAsync(existing);
        }
        else
        {
            await _connectionRepo.AddAsync(new Connection
            {
                UserId = request.UserId,
                Provider = provider,
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken,
                ExternalAccountId = request.ExternalAccountId
            });
        }

        await _connectionRepo.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("status/{userId:guid}")]
    public async Task<ActionResult<List<ConnectionStatusDto>>> Status(Guid userId)
    {
        var connections = await _connectionRepo.GetActiveByUserAsync(userId);

        var all = Enum.GetValues<ConnectionProvider>().Select(p =>
        {
            var match = connections.FirstOrDefault(c => c.Provider == p);
            return new ConnectionStatusDto(p.ToString(), match is not null, match?.ConnectedAt);
        }).ToList();

        return Ok(all);
    }

    [HttpDelete("{userId:guid}/{provider}")]
    public async Task<IActionResult> Disconnect(Guid userId, string provider)
    {
        if (!Enum.TryParse<ConnectionProvider>(provider, true, out var p)) return BadRequest();
        var conn = await _connectionRepo.GetByUserAndProviderAsync(userId, p);
        if (conn is null) return NotFound();
        conn.IsActive = false;
        await _connectionRepo.UpdateAsync(conn);
        await _connectionRepo.SaveChangesAsync();
        return NoContent();
    }
}
