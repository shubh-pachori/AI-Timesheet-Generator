using AITimesheet.API.Entities;
using AITimesheet.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.API.Controllers;

/// <summary>Manager dashboard analytics: weekly productivity graphs, approval funnel.</summary>
[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AnalyticsController(AppDbContext db) => _db = db;

    [HttpGet("team/{managerId:guid}")]
    public async Task<IActionResult> GetTeamAnalytics(Guid managerId, CancellationToken ct)
    {
        var teamUserIds = await _db.Users.Where(u => u.ManagerId == managerId).Select(u => u.Id).ToListAsync(ct);

        var timesheets = await _db.Timesheets
            .Where(t => teamUserIds.Contains(t.UserId))
            .Include(t => t.Entries)
            .Include(t => t.User)
            .ToListAsync(ct);

        var byStatus = timesheets.GroupBy(t => t.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var weeklyHours = timesheets
            .GroupBy(t => t.WeekStartDate)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                week = g.Key,
                totalHours = g.SelectMany(t => t.Entries).Sum(e => e.Hours)
            });

        var perEmployee = timesheets
            .GroupBy(t => t.User!.FullName)
            .Select(g => new
            {
                employee = g.Key,
                totalHours = g.SelectMany(t => t.Entries).Sum(e => e.Hours),
                submitted = g.Count(t => t.Status != TimesheetStatus.Draft)
            });

        return Ok(new { byStatus, weeklyHours, perEmployee });
    }
}
