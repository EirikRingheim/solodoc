using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.TaskGroups;

public class TaskGroupProcedure : BaseEntity
{
    public Guid TaskGroupId { get; set; }
    public Guid ProcedureTemplateId { get; set; }

    // Navigation
    public TaskGroup TaskGroup { get; set; } = null!;
}
