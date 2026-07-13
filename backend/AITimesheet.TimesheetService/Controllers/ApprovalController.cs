using AITimesheet.TimesheetService.DTOs;
using AITimesheet.TimesheetService.Entities;
using AITimesheet.TimesheetService.RepositoryLayer.Interfaces;
using AITimesheet.TimesheetService.ServiceLayer.Clients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITimesheet.TimesheetService.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalController : ControllerBase
{
    private readonly ITimesheetRepository _repo;
    private readonly IdentityServiceClient _identityClient;

    public ApprovalController(ITimesheetRepository repo, IdentityServiceClient identityClient)
    {
        _repo = repo;
        _identityClient = identityClient;
    }

    [HttpGet("pending/{managerId:guid}")]
    public async Task<ActionResult<List<TimesheetDto>>> GetPending(Guid managerId, CancellationToken ct)
    {
        // 1. Fetch reporting team members from Identity Service
        var teamMembers = await _identityClient.GetEmployeesByManagerIdAsync(managerId, ct);
        var memberIds = teamMembers.Select(u => u.Id).ToList();

        if (memberIds.Count == 0)
        {
            return Ok(new List<TimesheetDto>());
        }

        // 2. Fetch pending timesheets for these user IDs
        var sheets = await _repo.GetPendingApprovalsAsync(memberIds, ct);
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
