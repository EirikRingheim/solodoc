using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.TaskGroups;

namespace Solodoc.Infrastructure.Persistence.Configurations.TaskGroups;

public class TaskGroupChecklistConfiguration : IEntityTypeConfiguration<TaskGroupChecklist>
{
    public void Configure(EntityTypeBuilder<TaskGroupChecklist> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TaskGroupId);
        builder.HasIndex(t => t.ChecklistTemplateId);
    }
}
