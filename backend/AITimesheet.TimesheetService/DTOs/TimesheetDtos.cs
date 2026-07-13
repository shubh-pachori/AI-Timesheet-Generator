namespace AITimesheet.TimesheetService.DTOs;

// Activity DTOs
public record ActivityDto(Guid Id, string Source, string Title, string? Status, DateOnly ActivityDate, double? EstimatedHours);

// Chat DTOs
public record ChatRequest(Guid UserId, string Question);
public record ChatResponse(string Answer);

// Connection DTOs
public record ConnectAccountRequest(Guid UserId, string Provider, string AccessToken, string? RefreshToken, string? ExternalAccountId);
public record ConnectionStatusDto(string Provider, bool IsConnected, DateTime? ConnectedAt);

// Timesheet DTOs
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
