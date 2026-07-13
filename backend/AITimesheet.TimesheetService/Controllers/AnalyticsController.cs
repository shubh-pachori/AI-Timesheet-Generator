using AITimesheet.TimesheetService.Entities;
using AITimesheet.TimesheetService.RepositoryLayer.Interfaces;
using AITimesheet.TimesheetService.ServiceLayer.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITimesheet.TimesheetService.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ITimesheetRepository _repo;
    private readonly IdentityServiceClient _identityClient;

    public AnalyticsController(ITimesheetRepository repo, IdentityServiceClient identityClient)
    {
        _repo = repo;
        _identityClient = identityClient;
    }

    [HttpGet("team/{managerId:guid}")]
    public async Task<IActionResult> GetTeamAnalytics(Guid managerId, CancellationToken ct)
    {
        // 1. Fetch team members reporting to manager
        var teamMembers = await _identityClient.GetEmployeesByManagerIdAsync(managerId, ct);
        var teamUserIds = teamMembers.Select(u => u.Id).ToList();

        if (teamUserIds.Count == 0)
        {
            return Ok(new
            {
                byStatus = new Dictionary<string, int>(),
                weeklyHours = new List<object>(),
                perEmployee = new List<object>()
            });
        }

        // 2. Fetch all timesheets for team
        var timesheets = await _repo.GetByUsersAsync(teamUserIds, ct);

        var byStatus = timesheets.GroupBy(t => t.Status)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var weeklyHours = timesheets
            .GroupBy(t => t.WeekStartDate)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                week = g.Key,
                totalHours = g.SelectMany(t => t.Entries).Sum(e => e.Hours)
            }).ToList();

        var employeeMap = teamMembers.ToDictionary(u => u.Id, u => u.FullName);

        var perEmployee = timesheets
            .GroupBy(t => t.UserId)
            .Select(g => new
            {
                employee = employeeMap.TryGetValue(g.Key, out var name) ? name : "Unknown Employee",
                totalHours = g.SelectMany(t => t.Entries).Sum(e => e.Hours),
                submitted = g.Count(t => t.Status != TimesheetStatus.Draft)
            }).ToList();

        return Ok(new { byStatus, weeklyHours, perEmployee });
    }
}
