using AITimesheet.API.DTOs;
using AITimesheet.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.API.Controllers;

[ApiController]
[Route("api/activities")]
public class ActivityController : ControllerBase
{
    private readonly AppDbContext _db;
    public ActivityController(AppDbContext db) => _db = db;

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<ActivityDto>>> GetForUser(Guid userId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        var query = _db.Activities.Where(a => a.UserId == userId);
        if (from.HasValue) query = query.Where(a => a.ActivityDate >= from.Value);
        if (to.HasValue) query = query.Where(a => a.ActivityDate <= to.Value);

        var results = await query.OrderByDescending(a => a.ActivityDate).ToListAsync(ct);
        return Ok(results.Select(a => new ActivityDto(a.Id, a.Source.ToString(), a.Title, a.Status, a.ActivityDate, a.EstimatedHours)).ToList());
    }
}
