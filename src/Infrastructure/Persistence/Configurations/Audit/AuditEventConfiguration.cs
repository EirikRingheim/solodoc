using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Audit;

namespace Solodoc.Infrastructure.Persistence.Configurations.Audit;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
        builder.HasIndex(a => a.PerformedById);
        builder.HasIndex(a => a.PerformedAt);

        builder.Property(a => a.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
    }
}
