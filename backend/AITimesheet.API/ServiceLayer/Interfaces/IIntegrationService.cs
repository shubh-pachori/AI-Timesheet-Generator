using AITimesheet.API.Entities;

namespace AITimesheet.API.Interfaces;

/// <summary>
/// Common contract for all external data-source integrations
/// (GitHub, Azure DevOps, Jira, Outlook/Teams Calendar).
/// Each implementation fetches raw activity for a user for a given week
/// and normalizes it into Activity records.
/// </summary>
public interface IIntegrationService
{
    ConnectionProvider Provider { get; }

    Task<List<Activity>> FetchActivitiesAsync(
        Guid userId,
        string accessToken,
        DateOnly weekStart,
        DateOnly weekEnd,
        CancellationToken ct = default);
}
