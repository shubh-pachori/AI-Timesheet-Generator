using AITimesheet.API.Entities;

namespace AITimesheet.API.Interfaces;

public interface ITimesheetRepository
{
    Task<Timesheet?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Timesheet>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<Timesheet>> GetPendingApprovalsAsync(Guid managerId, CancellationToken ct = default);
    Task AddAsync(Timesheet timesheet, CancellationToken ct = default);
    Task UpdateAsync(Timesheet timesheet, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
