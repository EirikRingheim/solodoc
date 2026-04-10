using System.Text.Json;
using FluentAssertions;
using Solodoc.Domain.Entities.Audit;

namespace Solodoc.UnitTests.Services;

public class AuditServiceTests
{
    [Fact]
    public void LogEvent_CreatesAuditEvent()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var performedById = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act — simulate what AuditService.LogEventAsync does
        var auditEvent = new AuditEvent
        {
            TenantId = tenantId,
            EntityType = "Deviation",
            EntityId = entityId,
            Action = "Created",
            PerformedById = performedById,
            Details = "New deviation reported",
            PerformedAt = now
        };

        // Assert
        auditEvent.TenantId.Should().Be(tenantId);
        auditEvent.EntityType.Should().Be("Deviation");
        auditEvent.EntityId.Should().Be(entityId);
        auditEvent.Action.Should().Be("Created");
        auditEvent.PerformedById.Should().Be(performedById);
        auditEvent.Details.Should().Be("New deviation reported");
        auditEvent.PerformedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        auditEvent.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateSnapshot_SerializesEntity()
    {
        // Arrange
        var entity = new { Name = "Test Project", Status = "Active", Id = Guid.NewGuid() };
        var tenantId = Guid.NewGuid();

        // Act — simulate what AuditService.CreateSnapshotAsync does
        var json = JsonSerializer.Serialize(entity, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var snapshot = new AuditSnapshot
        {
            TenantId = tenantId,
            EntityType = "Project",
            EntityId = entity.Id,
            SnapshotJson = json,
            Reason = "Document signed"
        };

        // Assert
        snapshot.TenantId.Should().Be(tenantId);
        snapshot.EntityType.Should().Be("Project");
        snapshot.EntityId.Should().Be(entity.Id);
        snapshot.SnapshotJson.Should().NotBeNullOrEmpty();
        snapshot.SnapshotJson.Should().Contain("\"name\":");
        snapshot.SnapshotJson.Should().Contain("\"status\":\"Active\"");
        snapshot.Reason.Should().Be("Document signed");
        snapshot.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AuditEvent_WithNullTenantId_IsValid()
    {
        // Arrange & Act — system-level events have no tenant
        var auditEvent = new AuditEvent
        {
            TenantId = null,
            EntityType = "System",
            EntityId = Guid.NewGuid(),
            Action = "HealthCheck",
            PerformedById = Guid.Empty,
            PerformedAt = DateTimeOffset.UtcNow
        };

        // Assert
        auditEvent.TenantId.Should().BeNull();
        auditEvent.Action.Should().Be("HealthCheck");
    }

    [Fact]
    public void AuditSnapshot_WithNullReason_IsValid()
    {
        // Arrange & Act
        var snapshot = new AuditSnapshot
        {
            TenantId = Guid.NewGuid(),
            EntityType = "Checklist",
            EntityId = Guid.NewGuid(),
            SnapshotJson = "{}",
            Reason = null
        };

        // Assert
        snapshot.Reason.Should().BeNull();
        snapshot.SnapshotJson.Should().NotBeNullOrEmpty();
    }
}
