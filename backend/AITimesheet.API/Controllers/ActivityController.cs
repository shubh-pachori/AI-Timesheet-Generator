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
        if (from.HasValue)
        {
            var fromDt = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(a => a.ActivityDate >= fromDt);
        }
        if (to.HasValue)
        {
            var toDt = to.Value.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(a => a.ActivityDate <= toDt);
        }

        var results = await query.OrderByDescending(a => a.ActivityDate).ToListAsync(ct);
        return Ok(results.Select(a => new ActivityDto(a.Id, a.Source.ToString(), a.Title, a.Status, DateOnly.FromDateTime(a.ActivityDate), a.EstimatedHours)).ToList());
    }
}
