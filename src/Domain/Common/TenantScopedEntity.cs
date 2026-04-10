namespace Solodoc.Domain.Common;

public abstract class TenantScopedEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
