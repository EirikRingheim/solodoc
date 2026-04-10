using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.TaskGroups;

namespace Solodoc.Infrastructure.Persistence.Configurations.TaskGroups;

public class TaskGroupChemicalConfiguration : IEntityTypeConfiguration<TaskGroupChemical>
{
    public void Configure(EntityTypeBuilder<TaskGroupChemical> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TaskGroupId);
        builder.HasIndex(t => t.ChemicalId);
    }
}
