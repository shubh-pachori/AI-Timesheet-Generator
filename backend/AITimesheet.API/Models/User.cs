namespace AITimesheet.API.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AzureAdObjectId { get; set; }
    public string Role { get; set; } = "Employee"; // Employee | Manager | Admin
    public Guid? ManagerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Connection> Connections { get; set; } = new List<Connection>();
    public ICollection<Timesheet> Timesheets { get; set; } = new List<Timesheet>();
}
