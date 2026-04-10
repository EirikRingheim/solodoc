using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Hours;

public class TimeEntry : TenantScopedEntity
{
    public Guid PersonId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? JobId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; }
    public string? Description { get; set; }

    public TimeEntryCategory Category { get; set; } = TimeEntryCategory.Arbeid;
    public TimeEntryStatus Status { get; set; } = TimeEntryStatus.Draft;

    public DateTimeOffset? ClockIn { get; set; }
    public DateTimeOffset? ClockOut { get; set; }
    public DateTimeOffset? StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }

    public int BreakMinutes { get; set; }
    public decimal OvertimeHours { get; set; }

    public double? GpsLatitudeIn { get; set; }
    public double? GpsLongitudeIn { get; set; }
    public double? GpsLatitudeOut { get; set; }
    public double? GpsLongitudeOut { get; set; }

    public string? Notes { get; set; }
    public bool IsManual { get; set; }

    // Navigation
    public ICollection<TimeEntryAllowance> Allowances { get; set; } = [];
}
