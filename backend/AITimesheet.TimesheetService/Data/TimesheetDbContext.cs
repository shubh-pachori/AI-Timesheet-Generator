using AITimesheet.TimesheetService.Entities;
using Microsoft.EntityFrameworkCore;

namespace AITimesheet.TimesheetService.Data;

public class TimesheetDbContext : DbContext
{
    public TimesheetDbContext(DbContextOptions<TimesheetDbContext> options) : base(options) { }

    public DbSet<Connection> Connections => Set<Connection>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<Approval> Approvals => Set<Approval>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Snake_case table names for Postgres friendliness
        modelBuilder.Entity<Connection>().ToTable("connections");
        modelBuilder.Entity<Activity>().ToTable("activities");
        modelBuilder.Entity<Timesheet>().ToTable("timesheets");
        modelBuilder.Entity<TimesheetEntry>().ToTable("timesheet_entries");
        modelBuilder.Entity<Approval>().ToTable("approvals");

        modelBuilder.Entity<Connection>()
            .HasIndex(c => c.UserId);

        modelBuilder.Entity<Timesheet>()
            .HasIndex(t => t.UserId);

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

        // Enums mapping to strings in Postgres
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
        // Force UTC on all DateTime columns to make Npgsql happy
        var dateTimeConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
            }
        }
    }
}
