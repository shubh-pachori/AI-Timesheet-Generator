using AITimesheet.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use snake_case-ish table names for Postgres friendliness
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Connection>().ToTable("connections");
        modelBuilder.Entity<Activity>().ToTable("activities");
        modelBuilder.Entity<Timesheet>().ToTable("timesheets");
        modelBuilder.Entity<TimesheetEntry>().ToTable("timesheet_entries");
        modelBuilder.Entity<Approval>().ToTable("approvals");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Connection>()
            .HasOne(c => c.User)
            .WithMany(u => u.Connections)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Timesheet>()
            .HasOne(t => t.User)
            .WithMany(u => u.Timesheets)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TimesheetEntry>()
            .HasOne(e => e.Timesheet)
            .WithMany(t => t.Entries)
            .HasForeignKey(e => e.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Approval>()
            .HasOne(a => a.Timesheet)
            .WithOne(t => t.Approval)
            .HasForeignKey<Approval>(a => a.TimesheetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store enums as strings for readability in Postgres
        modelBuilder.Entity<Connection>()
            .Property(c => c.Provider)
            .HasConversion<string>();

        modelBuilder.Entity<Activity>()
            .Property(a => a.Source)
            .HasConversion<string>();

        modelBuilder.Entity<Timesheet>()
            .Property(t => t.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Approval>()
            .Property(a => a.Status)
            .HasConversion<string>();
    }
}
