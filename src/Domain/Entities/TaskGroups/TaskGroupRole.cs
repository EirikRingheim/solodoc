using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.TaskGroups;

public class TaskGroupRole : BaseEntity
{
    public Guid TaskGroupId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    // Navigation
    public TaskGroup TaskGroup { get; set; } = null!;
}
