using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Solodoc.Infrastructure.Persistence.Configurations.Equipment;

public class EquipmentConfiguration : IEntityTypeConfiguration<Domain.Entities.Equipment.Equipment>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Equipment.Equipment> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.Name).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Type).HasMaxLength(200);
        builder.Property(e => e.RegistrationNumber).HasMaxLength(50);
        builder.Property(e => e.SerialNumber).HasMaxLength(100);
        builder.Property(e => e.Make).HasMaxLength(200);
        builder.Property(e => e.Model).HasMaxLength(200);

        builder.HasMany(e => e.MaintenanceRecords)
            .WithOne(m => m.Equipment)
            .HasForeignKey(m => m.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Inspections)
            .WithOne(i => i.Equipment)
            .HasForeignKey(i => i.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ProjectAssignments)
            .WithOne(a => a.Equipment)
            .HasForeignKey(a => a.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
