using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Equipment;

namespace Solodoc.Infrastructure.Persistence.Configurations.Equipment;

public class EquipmentProjectAssignmentConfiguration : IEntityTypeConfiguration<EquipmentProjectAssignment>
{
    public void Configure(EntityTypeBuilder<EquipmentProjectAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.EquipmentId);
        builder.HasIndex(a => a.ProjectId);
    }
}
