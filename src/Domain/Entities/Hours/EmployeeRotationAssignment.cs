using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class EmployeeRotationAssignment : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid RotationPatternId { get; set; }
    public DateOnly CycleStartDate { get; set; }     // When day 1 of the cycle falls
    public DateOnly? EffectiveTo { get; set; }        // null = still active

    // Navigation
    public RotationPattern RotationPattern { get; set; } = null!;
}
