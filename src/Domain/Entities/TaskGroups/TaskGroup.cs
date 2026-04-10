using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.TaskGroups;

public class TaskGroup : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public ICollection<TaskGroupChecklist> Checklists { get; set; } = [];
    public ICollection<TaskGroupEquipment> Equipment { get; set; } = [];
    public ICollection<TaskGroupProcedure> Procedures { get; set; } = [];
    public ICollection<TaskGroupChemical> Chemicals { get; set; } = [];
    public ICollection<TaskGroupRole> Roles { get; set; } = [];
}
