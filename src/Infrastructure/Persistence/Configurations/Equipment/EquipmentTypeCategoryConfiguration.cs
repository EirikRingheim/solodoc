using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Equipment;

namespace Solodoc.Infrastructure.Persistence.Configurations.Equipment;

public class EquipmentTypeCategoryConfiguration : IEntityTypeConfiguration<EquipmentTypeCategory>
{
    public void Configure(EntityTypeBuilder<EquipmentTypeCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => new { c.TenantId, c.Name }).IsUnique();
        builder.HasIndex(c => new { c.TenantId, c.SortOrder });

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
    }
}
