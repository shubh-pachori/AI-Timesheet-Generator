using AITimesheet.API.DTOs;
using AITimesheet.API.Entities;
using AITimesheet.API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AITimesheet.API.Controllers;

/// <summary>Step 7: Manager dashboard — review, approve, or reject submitted timesheets.</summary>
[ApiController]
[Route("api/approvals")]
public class ApprovalController : ControllerBase
{
    private readonly ITimesheetRepository _repo;

    public ApprovalController(ITimesheetRepository repo) => _repo = repo;

    [HttpGet("pending/{managerId:guid}")]
    public async Task<ActionResult<List<TimesheetDto>>> GetPending(Guid managerId, CancellationToken ct)
    {
        var sheets = await _repo.GetPendingApprovalsAsync(managerId, ct);
        return Ok(sheets.Select(t => new TimesheetDto(
            t.Id, t.UserId, t.WeekStartDate, t.WeekEndDate, t.Status.ToString(), t.AiWeeklySummary,
            t.Entries.Select(e => new TimesheetEntryDto(e.Id, e.EntryDate, e.ActivityDescription, e.Hours, e.DevelopmentHours, e.MeetingHours, e.ReviewHours, e.IsEdited)).ToList()
        )).ToList());
    }

    [HttpPost("{timesheetId:guid}/decision")]
    public async Task<IActionResult> Decide(Guid timesheetId, [FromBody] ApprovalDecisionRequest request, CancellationToken ct)
    {
        var sheet = await _repo.GetByIdAsync(timesheetId, ct);
        if (sheet?.Approval is null) return NotFound();

        sheet.Approval.Status = request.Approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        sheet.Approval.Comments = request.Comments;
        sheet.Approval.DecidedAt = DateTime.UtcNow;
        sheet.Status = request.Approve ? TimesheetStatus.Approved : TimesheetStatus.Rejected;

        await _repo.UpdateAsync(sheet, ct);
        await _repo.SaveChangesAsync(ct);
        return NoContent();
    }
}
