using System.Text.Json;
using Microsoft.Extensions.Logging;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Audit;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.Infrastructure.Services;

public class AuditService(SolodocDbContext db, ILogger<AuditService> logger) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task LogEventAsync(
        Guid? tenantId,
        string entityType,
        Guid entityId,
        string action,
        Guid performedById,
        string? details = null,
        CancellationToken ct = default)
    {
        var auditEvent = new AuditEvent
        {
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PerformedById = performedById,
            Details = details,
            PerformedAt = DateTimeOffset.UtcNow
        };

        db.AuditEvents.Add(auditEvent);
        await db.SaveChangesAsync(ct);

        logger.LogDebug("Audit event logged: {Action} on {EntityType}/{EntityId} by {PerformedById}",
            action, entityType, entityId, performedById);
    }

    public async Task CreateSnapshotAsync(
        Guid? tenantId,
        string entityType,
        Guid entityId,
        object entity,
        string? reason = null,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(entity, entity.GetType(), JsonOptions);

        var snapshot = new AuditSnapshot
        {
            TenantId = tenantId,
            EntityType = entityType,
            EntityId = entityId,
            SnapshotJson = json,
            Reason = reason
        };

        db.AuditSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);

        logger.LogDebug("Audit snapshot created for {EntityType}/{EntityId}: {Reason}",
            entityType, entityId, reason ?? "No reason");
    }
}
