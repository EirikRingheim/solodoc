using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.TaskGroups;

public class TaskGroupChecklist : BaseEntity
{
    public Guid TaskGroupId { get; set; }
    public Guid ChecklistTemplateId { get; set; }

    // Navigation
    public TaskGroup TaskGroup { get; set; } = null!;
}
