using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.TaskGroups;

namespace Solodoc.Infrastructure.Persistence.Configurations.TaskGroups;

public class TaskGroupRoleConfiguration : IEntityTypeConfiguration<TaskGroupRole>
{
    public void Configure(EntityTypeBuilder<TaskGroupRole> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TaskGroupId);

        builder.Property(t => t.RoleName).HasMaxLength(200).IsRequired();
    }
}
