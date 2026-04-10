using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.TaskGroups;

namespace Solodoc.Infrastructure.Persistence.Configurations.TaskGroups;

public class TaskGroupEquipmentConfiguration : IEntityTypeConfiguration<TaskGroupEquipment>
{
    public void Configure(EntityTypeBuilder<TaskGroupEquipment> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TaskGroupId);
        builder.HasIndex(t => t.EquipmentId);
    }
}
