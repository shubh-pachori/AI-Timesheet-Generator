using AITimesheet.TimesheetService.DTOs;
using AITimesheet.TimesheetService.RepositoryLayer.Interfaces;
using AITimesheet.TimesheetService.ServiceLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITimesheet.TimesheetService.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IActivityRepository _activityRepo;
    private readonly IAiTimesheetService _ai;

    public ChatController(IActivityRepository activityRepo, IAiTimesheetService ai)
    {
        _activityRepo = activityRepo;
        _ai = ai;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var activities = await _activityRepo.GetForUserAsync(request.UserId, since, null, ct);

        var answer = await _ai.AnswerChatQueryAsync(request.UserId, request.Question, activities, ct);
        return Ok(new ChatResponse(answer));
    }
}
