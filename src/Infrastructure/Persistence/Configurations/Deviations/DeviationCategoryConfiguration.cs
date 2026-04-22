using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Deviations;

namespace Solodoc.Infrastructure.Persistence.Configurations.Deviations;

public class DeviationCategoryConfiguration : IEntityTypeConfiguration<DeviationCategory>
{
    public void Configure(EntityTypeBuilder<DeviationCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.Name }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.SortOrder });

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
    }
}
