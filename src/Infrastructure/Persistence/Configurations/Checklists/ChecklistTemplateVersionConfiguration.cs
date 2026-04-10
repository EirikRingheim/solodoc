using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Checklists;

namespace Solodoc.Infrastructure.Persistence.Configurations.Checklists;

public class ChecklistTemplateVersionConfiguration : IEntityTypeConfiguration<ChecklistTemplateVersion>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplateVersion> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.ChecklistTemplateId, v.VersionNumber }).IsUnique();

        builder.HasOne(v => v.ChecklistTemplate)
            .WithMany()
            .HasForeignKey(v => v.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Items)
            .WithOne(i => i.TemplateVersion)
            .HasForeignKey(i => i.TemplateVersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
