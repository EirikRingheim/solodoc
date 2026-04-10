using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.TaskGroups;

namespace Solodoc.Infrastructure.Persistence.Configurations.TaskGroups;

public class TaskGroupProcedureConfiguration : IEntityTypeConfiguration<TaskGroupProcedure>
{
    public void Configure(EntityTypeBuilder<TaskGroupProcedure> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TaskGroupId);
        builder.HasIndex(t => t.ProcedureTemplateId);
    }
}
