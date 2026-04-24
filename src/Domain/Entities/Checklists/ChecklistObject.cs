using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Checklists;

/// <summary>
/// A named, numbered object within a project that has checklists tied to it.
/// E.g., "Fundament 1", "Søyle 3", "Vindu 12"
/// </summary>
public class ChecklistObject : TenantScopedEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Fundament"
    public int Number { get; set; } // e.g., 1, 2, 3
    public string DisplayName => $"{Name} {Number}";

    // Navigation
    public ICollection<ChecklistObjectTemplate> Templates { get; set; } = [];
    public ICollection<ChecklistInstance> Instances { get; set; } = [];
}
