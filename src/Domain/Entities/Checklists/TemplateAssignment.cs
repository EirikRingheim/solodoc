using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Checklists;

/// <summary>
/// Assigns a checklist/schema/procedure template to a project, job, or location.
/// Workers see assigned templates when they open that project/location.
/// </summary>
public class TemplateAssignment : TenantScopedEntity
{
    public Guid ChecklistTemplateId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? LocationId { get; set; }

    // Navigation
    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
}
