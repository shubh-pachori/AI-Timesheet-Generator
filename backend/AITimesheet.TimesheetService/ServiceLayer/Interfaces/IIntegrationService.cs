using AITimesheet.TimesheetService.Entities;

namespace AITimesheet.TimesheetService.ServiceLayer.Interfaces;

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
