using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Audit;

public class AuditEvent : BaseEntity
{
    public Guid? TenantId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid PerformedById { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset PerformedAt { get; set; }
}
