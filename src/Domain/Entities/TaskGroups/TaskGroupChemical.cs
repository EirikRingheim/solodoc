using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.TaskGroups;

public class TaskGroupChemical : BaseEntity
{
    public Guid TaskGroupId { get; set; }
    public Guid ChemicalId { get; set; }

    // Navigation
    public TaskGroup TaskGroup { get; set; } = null!;
}
