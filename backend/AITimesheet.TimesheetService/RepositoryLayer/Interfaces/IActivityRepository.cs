using AITimesheet.TimesheetService.Entities;

namespace AITimesheet.TimesheetService.RepositoryLayer.Interfaces;

public interface IActivityRepository
{
    Task<List<Activity>> GetForUserAsync(Guid userId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default);
    Task AddRangeAsync(List<Activity> activities, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
