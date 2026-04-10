using Solodoc.Application.Common;

namespace Solodoc.Infrastructure.Auth;

public class TenantProvider : ITenantProvider
{
    public Guid? TenantId { get; private set; }

    public void SetTenantId(Guid tenantId)
    {
        TenantId = tenantId;
    }
}
