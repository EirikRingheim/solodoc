using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class EmployeeScheduleAssignment : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid WorkScheduleId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    // Navigation
    public WorkSchedule WorkSchedule { get; set; } = null!;
}
