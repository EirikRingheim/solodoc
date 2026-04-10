using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Equipment;

namespace Solodoc.Infrastructure.Persistence.Configurations.Equipment;

public class EquipmentInspectionConfiguration : IEntityTypeConfiguration<EquipmentInspection>
{
    public void Configure(EntityTypeBuilder<EquipmentInspection> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.EquipmentId);

        builder.Property(i => i.Type).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Status).HasMaxLength(50).IsRequired();
        builder.Property(i => i.FileKey).HasMaxLength(500);
        builder.Property(i => i.Notes).HasMaxLength(2000);
    }
}
