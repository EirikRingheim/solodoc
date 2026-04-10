using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Equipment;

namespace Solodoc.Infrastructure.Persistence.Configurations.Equipment;

public class EquipmentMaintenanceConfiguration : IEntityTypeConfiguration<EquipmentMaintenance>
{
    public void Configure(EntityTypeBuilder<EquipmentMaintenance> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.EquipmentId);

        builder.Property(m => m.Description).HasMaxLength(2000).IsRequired();
        builder.Property(m => m.Cost).HasPrecision(18, 2);
        builder.Property(m => m.Notes).HasMaxLength(2000);
    }
}
