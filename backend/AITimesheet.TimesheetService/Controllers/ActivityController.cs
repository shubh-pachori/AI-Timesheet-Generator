using AITimesheet.TimesheetService.DTOs;
using AITimesheet.TimesheetService.RepositoryLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITimesheet.TimesheetService.Controllers;

[ApiController]
[Route("api/activities")]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly IActivityRepository _activityRepo;

    public ActivityController(IActivityRepository activityRepo)
    {
        _activityRepo = activityRepo;
    }

    [HttpGet("user/{userId:guid}")]
    public async Task<ActionResult<List<ActivityDto>>> GetForUser(Guid userId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct)
    {
        DateTime? fromDt = from?.ToDateTime(TimeOnly.MinValue);
        DateTime? toDt = to?.ToDateTime(TimeOnly.MaxValue);

        var results = await _activityRepo.GetForUserAsync(userId, fromDt, toDt, ct);
        return Ok(results.Select(a => new ActivityDto(a.Id, a.Source.ToString(), a.Title, a.Status, DateOnly.FromDateTime(a.ActivityDate), a.EstimatedHours)).ToList());
    }
}
