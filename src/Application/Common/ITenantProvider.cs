namespace Solodoc.Application.Common;

public interface ITenantProvider
{
    Guid? TenantId { get; }
    void SetTenantId(Guid tenantId);
}
