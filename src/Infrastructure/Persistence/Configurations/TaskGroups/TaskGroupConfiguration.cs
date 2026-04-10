using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.TaskGroups;

namespace Solodoc.Infrastructure.Persistence.Configurations.TaskGroups;

public class TaskGroupConfiguration : IEntityTypeConfiguration<TaskGroup>
{
    public void Configure(EntityTypeBuilder<TaskGroup> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TenantId);

        builder.Property(t => t.Name).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.HasMany(t => t.Checklists)
            .WithOne(c => c.TaskGroup)
            .HasForeignKey(c => c.TaskGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Equipment)
            .WithOne(e => e.TaskGroup)
            .HasForeignKey(e => e.TaskGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Procedures)
            .WithOne(p => p.TaskGroup)
            .HasForeignKey(p => p.TaskGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Chemicals)
            .WithOne(c => c.TaskGroup)
            .HasForeignKey(c => c.TaskGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Roles)
            .WithOne(r => r.TaskGroup)
            .HasForeignKey(r => r.TaskGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
