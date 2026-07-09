using AITimesheet.API.Entities;

namespace AITimesheet.API.Interfaces;

public record AiGeneratedEntry(DateOnly Date, string Description, double Hours, double DevHours, double MeetingHours, double ReviewHours);

public record AiTimesheetResult(List<AiGeneratedEntry> Entries, string WeeklySummary, List<string> MissingHourPrompts);

public interface IAiTimesheetService
{
    /// <summary>
    /// Takes raw normalized activities for the week and produces
    /// human-readable daily timesheet entries + weekly summary.
    /// </summary>
    Task<AiTimesheetResult> GenerateTimesheetAsync(
        List<Activity> weekActivities,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken ct = default);

    Task<string> AnswerChatQueryAsync(
        Guid userId,
        string question,
        List<Activity> relevantActivities,
        CancellationToken ct = default);
}
