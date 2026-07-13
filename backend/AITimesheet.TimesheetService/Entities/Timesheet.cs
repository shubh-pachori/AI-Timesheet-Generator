namespace AITimesheet.TimesheetService.Entities;

public enum TimesheetStatus
{
    Draft,
    Generated,
    Submitted,
    Approved,
    Rejected
}

public class Timesheet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }
    public TimesheetStatus Status { get; set; } = TimesheetStatus.Draft;
    public string? AiWeeklySummary { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    public ICollection<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
    public Approval? Approval { get; set; }
}
