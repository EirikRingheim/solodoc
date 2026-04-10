using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class RotationPatternDay : BaseEntity
{
    public Guid RotationPatternId { get; set; }
    public int DayInCycle { get; set; }               // 1-based: day 1, day 2, etc.
    public Guid ShiftDefinitionId { get; set; }

    // Navigation
    public RotationPattern RotationPattern { get; set; } = null!;
    public ShiftDefinition ShiftDefinition { get; set; } = null!;
}
