using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class WorkSchedule : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal WeeklyHours { get; set; }
    public int DefaultBreakMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<WorkScheduleDay> Days { get; set; } = [];
}
