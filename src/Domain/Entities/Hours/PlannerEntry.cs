using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class PlannerEntry : TenantScopedEntity
{
    public Guid PersonId { get; set; }
    public DateOnly Date { get; set; }
    public Guid ShiftDefinitionId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? JobId { get; set; }

    // Navigation
    public ShiftDefinition ShiftDefinition { get; set; } = null!;
}
