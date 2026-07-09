using AITimesheet.API.Entities;
using AITimesheet.API.Interfaces;
using AITimesheet.API.Data;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.API.Repositories;

public class TimesheetRepository : ITimesheetRepository
{
    private readonly AppDbContext _db;

    public TimesheetRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Timesheet?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Timesheets
            .Include(t => t.Entries)
            .Include(t => t.Approval)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<List<Timesheet>> GetByUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Timesheets
            .Include(t => t.Entries)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.WeekStartDate)
            .ToListAsync(ct);

    public async Task<List<Timesheet>> GetPendingApprovalsAsync(Guid managerId, CancellationToken ct = default) =>
        await _db.Timesheets
            .Include(t => t.Entries)
            .Include(t => t.User)
            .Include(t => t.Approval)
            .Where(t => t.Status == TimesheetStatus.Submitted && t.User!.ManagerId == managerId)
            .ToListAsync(ct);

    public async Task AddAsync(Timesheet timesheet, CancellationToken ct = default) =>
        await _db.Timesheets.AddAsync(timesheet, ct);

    public Task UpdateAsync(Timesheet timesheet, CancellationToken ct = default)
    {
        _db.Timesheets.Update(timesheet);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);
}
