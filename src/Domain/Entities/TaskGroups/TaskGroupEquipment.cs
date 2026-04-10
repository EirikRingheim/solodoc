using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.TaskGroups;

public class TaskGroupEquipment : BaseEntity
{
    public Guid TaskGroupId { get; set; }
    public Guid EquipmentId { get; set; }

    // Navigation
    public TaskGroup TaskGroup { get; set; } = null!;
}
