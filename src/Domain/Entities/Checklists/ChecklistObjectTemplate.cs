using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Checklists;

/// <summary>
/// Links a checklist template to an object.
/// When an object is created, instances of these templates are auto-created.
/// </summary>
public class ChecklistObjectTemplate : BaseEntity
{
    public Guid ChecklistObjectId { get; set; }
    public Guid TemplateId { get; set; }
    public int SortOrder { get; set; }

    // Navigation
    public ChecklistObject Object { get; set; } = null!;
    public ChecklistTemplate Template { get; set; } = null!;
}
