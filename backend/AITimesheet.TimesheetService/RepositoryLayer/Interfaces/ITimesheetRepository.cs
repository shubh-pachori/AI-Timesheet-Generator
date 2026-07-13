using AITimesheet.TimesheetService.Entities;

namespace AITimesheet.TimesheetService.RepositoryLayer.Interfaces;

public interface ITimesheetRepository
{
    Task<Timesheet?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Timesheet>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<Timesheet>> GetByUsersAsync(List<Guid> userIds, CancellationToken ct = default);
    Task<List<Timesheet>> GetPendingApprovalsAsync(List<Guid> employeeIds, CancellationToken ct = default);
    Task AddAsync(Timesheet timesheet, CancellationToken ct = default);
    Task UpdateAsync(Timesheet timesheet, CancellationToken ct = default);
    Task DeleteAsync(Timesheet timesheet, CancellationToken ct = default);
    Task AddApprovalAsync(Approval approval, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    // Entry operations
    Task<TimesheetEntry?> GetEntryByIdAsync(Guid timesheetId, Guid entryId, CancellationToken ct = default);
}
