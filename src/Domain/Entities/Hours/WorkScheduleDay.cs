using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class WorkScheduleDay : BaseEntity
{
    public Guid WorkScheduleId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public WorkSchedule WorkSchedule { get; set; } = null!;
}
