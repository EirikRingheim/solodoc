using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Checklists;

public class ChecklistTemplate : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CurrentVersion { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
    public string? Tags { get; set; }
    public string? Category { get; set; }
    public string? DocumentNumber { get; set; }
    public bool RequireSignature { get; set; } = true;
    public int SignatureCount { get; set; } = 1;
    public string? SignatureRoles { get; set; }
    public string DocumentType { get; set; } = "Checklist"; // "Checklist", "Schema", "Procedure"
    public bool IsBaseTemplate { get; set; }
    public Guid? BaseTemplateId { get; set; }
    public bool IsLocked { get; set; }
}
