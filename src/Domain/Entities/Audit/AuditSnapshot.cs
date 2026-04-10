using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Audit;

public class AuditSnapshot : BaseEntity
{
    public Guid? TenantId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string? Reason { get; set; }
}
