using AITimesheet.API.DTOs;
using AITimesheet.API.Interfaces;
using AITimesheet.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.API.Controllers;

/// <summary>AI Chat: "What did I work on last Thursday?"</summary>
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IAiTimesheetService _ai;

    public ChatController(AppDbContext db, IAiTimesheetService ai)
    {
        _db = db;
        _ai = ai;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> Ask([FromBody] ChatRequest request, CancellationToken ct)
    {
        // Naive relevance filter for the hackathon demo: last 30 days of activity.
        var since = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var activities = await _db.Activities
            .Where(a => a.UserId == request.UserId && DateOnly.FromDateTime(a.ActivityDate) >= since)
            .ToListAsync(ct);

        var answer = await _ai.AnswerChatQueryAsync(request.UserId, request.Question, activities, ct);
        return Ok(new ChatResponse(answer));
    }
}
