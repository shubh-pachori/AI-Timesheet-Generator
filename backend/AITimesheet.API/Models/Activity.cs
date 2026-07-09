namespace AITimesheet.API.Entities;

public enum ActivitySource
{
    GitCommit,
    PullRequest,
    JiraTicket,
    Meeting,
    CodeReview
}

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public ActivitySource Source { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ExternalReference { get; set; } // e.g. commit SHA, ABC-123, PR#
    public string? Status { get; set; }
    public DateTime ActivityDate { get; set; }
    public double? EstimatedHours { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
