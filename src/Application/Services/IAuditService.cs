namespace Solodoc.Application.Services;

public interface IAuditService
{
    Task LogEventAsync(Guid? tenantId, string entityType, Guid entityId, string action, Guid performedById, string? details = null, CancellationToken ct = default);
    Task CreateSnapshotAsync(Guid? tenantId, string entityType, Guid entityId, object entity, string? reason = null, CancellationToken ct = default);
}
