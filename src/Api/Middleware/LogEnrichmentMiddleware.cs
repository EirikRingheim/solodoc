using System.Security.Claims;
using Serilog.Context;

namespace Solodoc.Api.Middleware;

public class LogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "none";
        var requestId = context.TraceIdentifier;

        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("TenantId", tenantId))
        using (LogContext.PushProperty("RequestId", requestId))
        {
            await next(context);
        }
    }
}
