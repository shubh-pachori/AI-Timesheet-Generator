namespace AITimesheet.IdentityService.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AzureAdObjectId { get; set; }
    public string Role { get; set; } = "Employee"; // Employee | Manager | Admin
    public Guid? ManagerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
