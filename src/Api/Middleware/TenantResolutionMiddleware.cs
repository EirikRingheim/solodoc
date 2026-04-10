using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.Api.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, SolodocDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var personIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                ?? context.User.FindFirstValue("sub");

            if (Guid.TryParse(personIdClaim, out var personId))
            {
                Guid? tenantId = null;

                // Check X-Tenant-Id header first (explicit tenant selection)
                if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId)
                    && Guid.TryParse(headerTenantId.FirstOrDefault(), out var headerGuid))
                {
                    // Verify the user actually has membership in this tenant
                    var hasAccess = await db.TenantMemberships
                        .AnyAsync(m => m.PersonId == personId
                            && m.TenantId == headerGuid
                            && m.State == TenantMembershipState.Active);

                    if (hasAccess)
                        tenantId = headerGuid;
                }

                // Fallback: first active membership
                tenantId ??= await db.TenantMemberships
                    .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
                    .Select(m => (Guid?)m.TenantId)
                    .FirstOrDefaultAsync();

                if (tenantId.HasValue)
                    tenantProvider.SetTenantId(tenantId.Value);
            }
        }

        await next(context);
    }
}
