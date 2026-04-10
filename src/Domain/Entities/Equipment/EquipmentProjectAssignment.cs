using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Equipment;

public class EquipmentProjectAssignment : BaseEntity
{
    public Guid EquipmentId { get; set; }
    public Guid ProjectId { get; set; }
    public DateOnly AssignedFrom { get; set; }
    public DateOnly? AssignedTo { get; set; }

    // Navigation
    public Equipment Equipment { get; set; } = null!;
}
