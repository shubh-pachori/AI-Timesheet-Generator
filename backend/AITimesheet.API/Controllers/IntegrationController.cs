using AITimesheet.API.DTOs;
using AITimesheet.API.Entities;
using AITimesheet.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.API.Controllers;

[ApiController]
[Route("api/integrations")]
public class IntegrationController : ControllerBase
{
    private readonly AppDbContext _db;

    public IntegrationController(AppDbContext db) => _db = db;

    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectAccountRequest request)
    {
        if (!Enum.TryParse<ConnectionProvider>(request.Provider, true, out var provider))
            return BadRequest($"Unknown provider '{request.Provider}'");

        var existing = await _db.Connections.FirstOrDefaultAsync(c =>
            c.UserId == request.UserId && c.Provider == provider);

        if (existing is not null)
        {
            existing.AccessToken = request.AccessToken;
            existing.RefreshToken = request.RefreshToken;
            existing.ExternalAccountId = request.ExternalAccountId;
            existing.IsActive = true;
        }
        else
        {
            _db.Connections.Add(new Connection
            {
                UserId = request.UserId,
                Provider = provider,
                AccessToken = request.AccessToken,
                RefreshToken = request.RefreshToken,
                ExternalAccountId = request.ExternalAccountId
            });
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("status/{userId:guid}")]
    public async Task<ActionResult<List<ConnectionStatusDto>>> Status(Guid userId)
    {
        var connections = await _db.Connections
            .Where(c => c.UserId == userId && c.IsActive)
            .ToListAsync();

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
        var conn = await _db.Connections.FirstOrDefaultAsync(c => c.UserId == userId && c.Provider == p);
        if (conn is null) return NotFound();
        conn.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
