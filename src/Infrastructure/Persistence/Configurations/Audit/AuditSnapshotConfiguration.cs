using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Audit;

namespace Solodoc.Infrastructure.Persistence.Configurations.Audit;

public class AuditSnapshotConfiguration : IEntityTypeConfiguration<AuditSnapshot>
{
    public void Configure(EntityTypeBuilder<AuditSnapshot> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });

        builder.Property(a => a.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.SnapshotJson).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(200);
    }
}
