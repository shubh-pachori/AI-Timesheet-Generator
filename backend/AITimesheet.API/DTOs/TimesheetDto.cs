namespace AITimesheet.API.DTOs;

public record TimesheetEntryDto(Guid Id, DateOnly Date, string Description, double Hours, double DevHours, double MeetingHours, double ReviewHours, bool IsEdited);

public record TimesheetDto(
    Guid Id,
    Guid UserId,
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    string Status,
    string? WeeklySummary,
    List<TimesheetEntryDto> Entries);

public record GenerateTimesheetRequest(Guid UserId, DateOnly WeekStartDate);

public record UpdateEntryRequest(double Hours, string Description);

public record ApprovalDecisionRequest(bool Approve, string? Comments);
