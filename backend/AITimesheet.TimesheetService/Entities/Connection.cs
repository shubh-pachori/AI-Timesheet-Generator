namespace AITimesheet.TimesheetService.Entities;

public enum ConnectionProvider
{
    GitHub,
    AzureDevOps,
    Jira,
    OutlookCalendar,
    TeamsCalendar
}

public class Connection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public ConnectionProvider Provider { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? ExternalAccountId { get; set; }
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
