using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Checklists;

namespace Solodoc.Infrastructure.Persistence.Configurations.Checklists;

public class ChecklistInstanceConfiguration : IEntityTypeConfiguration<ChecklistInstance>
{
    public void Configure(EntityTypeBuilder<ChecklistInstance> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => new { i.TenantId, i.Status });
        builder.HasIndex(i => i.ProjectId);

        builder.HasOne(i => i.TemplateVersion)
            .WithMany()
            .HasForeignKey(i => i.TemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne(ii => ii.Instance)
            .HasForeignKey(ii => ii.InstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
