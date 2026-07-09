using AITimesheet.API.DTOs;
using AITimesheet.API.Entities;
using AITimesheet.API.Interfaces;
using AITimesheet.API.Data;
using AITimesheet.API.Integrations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.API.Controllers;

[ApiController]
[Route("api/timesheets")]
public class TimesheetController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITimesheetRepository _repo;
    private readonly IAiTimesheetService _ai;
    private readonly IEnumerable<IIntegrationService> _integrations;

    public TimesheetController(
        AppDbContext db,
        ITimesheetRepository repo,
        IAiTimesheetService ai,
        IEnumerable<IIntegrationService> integrations)
    {
        _db = db;
        _repo = repo;
        _ai = ai;
        _integrations = integrations;
    }

    /// <summary>
    /// Step 3 + AI Processing: fetch activities from all connected sources for the week,
    /// run the AI engine, and persist a Generated timesheet ready for employee review.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<TimesheetDto>> Generate([FromBody] GenerateTimesheetRequest request, CancellationToken ct)
    {
        var weekStart = request.WeekStartDate;
        var weekEnd = weekStart.AddDays(6);

        var connections = await _db.Connections
            .Where(c => c.UserId == request.UserId && c.IsActive)
            .ToListAsync(ct);

        var allActivities = new List<Activity>();
        foreach (var conn in connections)
        {
            var service = _integrations.FirstOrDefault(i => i.Provider == conn.Provider);
            if (service is null) continue;
            var activities = await service.FetchActivitiesAsync(request.UserId, conn.AccessToken, weekStart, weekEnd, ct);
            allActivities.AddRange(activities);
        }

        // If no connections yet, still generate a demo timesheet using every mock integration
        // so users can see the feature before wiring up real accounts.
        if (connections.Count == 0)
        {
            foreach (var service in _integrations)
            {
                var activities = await service.FetchActivitiesAsync(request.UserId, string.Empty, weekStart, weekEnd, ct);
                allActivities.AddRange(activities);
            }
        }

        _db.Activities.AddRange(allActivities);

        var aiResult = await _ai.GenerateTimesheetAsync(allActivities, weekStart, weekEnd, ct);

        var timesheet = new Timesheet
        {
            UserId = request.UserId,
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            Status = TimesheetStatus.Generated,
            AiWeeklySummary = aiResult.WeeklySummary,
            Entries = aiResult.Entries.Select(e => new TimesheetEntry
            {
                EntryDate = e.Date,
                ActivityDescription = e.Description,
                Hours = e.Hours,
                DevelopmentHours = e.DevHours,
                MeetingHours = e.MeetingHours,
                ReviewHours = e.ReviewHours
            }).ToList()
        };

        await _repo.AddAsync(timesheet, ct);
        await _repo.SaveChangesAsync(ct);

        return Ok(ToDto(timesheet));
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<TimesheetDto>>> GetForUser(Guid userId, CancellationToken ct)
    {
        var sheets = await _repo.GetByUserAsync(userId, ct);
        return Ok(sheets.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TimesheetDto>> GetById(Guid id, CancellationToken ct)
    {
        var sheet = await _repo.GetByIdAsync(id, ct);
        if (sheet is null) return NotFound();
        return Ok(ToDto(sheet));
    }

    /// <summary>Step 5: Employee edits a specific day's entry before submitting.</summary>
    [HttpPut("{timesheetId:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateEntry(Guid timesheetId, Guid entryId, [FromBody] UpdateEntryRequest request, CancellationToken ct)
    {
        var entry = await _db.TimesheetEntries.FirstOrDefaultAsync(e => e.Id == entryId && e.TimesheetId == timesheetId, ct);
        if (entry is null) return NotFound();

        entry.Hours = request.Hours;
        entry.ActivityDescription = request.Description;
        entry.IsEdited = true;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Step 6: Submit for manager approval.</summary>
    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var sheet = await _repo.GetByIdAsync(id, ct);
        if (sheet is null) return NotFound();

        sheet.Status = TimesheetStatus.Submitted;
        sheet.SubmittedAt = DateTime.UtcNow;
        sheet.Approval = new Approval { TimesheetId = sheet.Id, Status = ApprovalStatus.Pending };

        await _repo.UpdateAsync(sheet, ct);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    private static TimesheetDto ToDto(Timesheet t) => new(
        t.Id, t.UserId, t.WeekStartDate, t.WeekEndDate, t.Status.ToString(), t.AiWeeklySummary,
        t.Entries.OrderBy(e => e.EntryDate).Select(e =>
            new TimesheetEntryDto(e.Id, e.EntryDate, e.ActivityDescription, e.Hours, e.DevelopmentHours, e.MeetingHours, e.ReviewHours, e.IsEdited)
        ).ToList());
}
