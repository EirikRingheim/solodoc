using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Procedures;

public class ProcedureTemplate : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int CurrentVersion { get; set; }
    public bool IsPublished { get; set; }
    public bool IsArchived { get; set; }
}
