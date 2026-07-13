using AITimesheet.TimesheetService.DTOs;
using AITimesheet.TimesheetService.Entities;
using AITimesheet.TimesheetService.RepositoryLayer.Interfaces;
using AITimesheet.TimesheetService.ServiceLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITimesheet.TimesheetService.Controllers;

[ApiController]
[Route("api/timesheets")]
[Authorize]
public class TimesheetController : ControllerBase
{
    private readonly ITimesheetRepository _repo;
    private readonly IConnectionRepository _connectionRepo;
    private readonly IActivityRepository _activityRepo;
    private readonly IAiTimesheetService _ai;
    private readonly IEnumerable<IIntegrationService> _integrations;

    public TimesheetController(
        ITimesheetRepository repo,
        IConnectionRepository connectionRepo,
        IActivityRepository activityRepo,
        IAiTimesheetService ai,
        IEnumerable<IIntegrationService> integrations)
    {
        _repo = repo;
        _connectionRepo = connectionRepo;
        _activityRepo = activityRepo;
        _ai = ai;
        _integrations = integrations;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<TimesheetDto>> Generate([FromBody] GenerateTimesheetRequest request, CancellationToken ct)
    {
        var weekStart = request.WeekStartDate;
        var weekEnd = weekStart.AddDays(6);

        // Prevent duplicates: Check if timesheet already exists for this week
        var existingSheets = await _repo.GetByUserAsync(request.UserId, ct);
        var existing = existingSheets.FirstOrDefault(t => t.WeekStartDate == weekStart);
        if (existing is not null)
        {
            if (existing.Status == TimesheetStatus.Submitted || 
                existing.Status == TimesheetStatus.Approved || 
                existing.Status == TimesheetStatus.Rejected)
            {
                return Ok(ToDto(existing));
            }
            
            // Delete old Draft/Generated timesheet so we can regenerate with fresh activities
            await _repo.DeleteAsync(existing, ct);
            await _repo.SaveChangesAsync(ct);
        }

        var connections = await _connectionRepo.GetActiveByUserAsync(request.UserId, ct);

        var allActivities = new List<Activity>();
        foreach (var conn in connections)
        {
            var service = _integrations.FirstOrDefault(i => i.Provider == conn.Provider);
            if (service is null) continue;
            var activities = await service.FetchActivitiesAsync(request.UserId, conn.AccessToken, weekStart, weekEnd, ct);
            allActivities.AddRange(activities);
        }

        if (connections.Count == 0)
        {
            foreach (var service in _integrations)
            {
                var activities = await service.FetchActivitiesAsync(request.UserId, string.Empty, weekStart, weekEnd, ct);
                allActivities.AddRange(activities);
            }
        }

        await _activityRepo.AddRangeAsync(allActivities, ct);
        await _activityRepo.SaveChangesAsync(ct);

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

    [HttpPut("{timesheetId:guid}/entries/{entryId:guid}")]
    public async Task<IActionResult> UpdateEntry(Guid timesheetId, Guid entryId, [FromBody] UpdateEntryRequest request, CancellationToken ct)
    {
        var entry = await _repo.GetEntryByIdAsync(timesheetId, entryId, ct);
        if (entry is null) return NotFound();

        entry.Hours = request.Hours;
        entry.ActivityDescription = request.Description;
        entry.IsEdited = true;

        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var sheet = await _repo.GetByIdAsync(id, ct);
        if (sheet is null) return NotFound();

        sheet.Status = TimesheetStatus.Submitted;
        sheet.SubmittedAt = DateTime.UtcNow;

        // Prevent DbUpdateConcurrencyException by reusing/updating the existing Approval record
        if (sheet.Approval is null)
        {
            var approval = new Approval { TimesheetId = sheet.Id, Status = ApprovalStatus.Pending };
            await _repo.AddApprovalAsync(approval, ct);
        }
        else
        {
            sheet.Approval.Status = ApprovalStatus.Pending;
            sheet.Approval.Comments = null;
            sheet.Approval.DecidedAt = null;
        }

        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }

    private static TimesheetDto ToDto(Timesheet t) => new(
        t.Id, t.UserId, t.WeekStartDate, t.WeekEndDate, t.Status.ToString(), t.AiWeeklySummary,
        t.Entries.OrderBy(e => e.EntryDate).Select(e =>
            new TimesheetEntryDto(e.Id, e.EntryDate, e.ActivityDescription, e.Hours, e.DevelopmentHours, e.MeetingHours, e.ReviewHours, e.IsEdited)
        ).ToList());
}
