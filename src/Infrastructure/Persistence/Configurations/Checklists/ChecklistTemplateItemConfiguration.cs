using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Checklists;

namespace Solodoc.Infrastructure.Persistence.Configurations.Checklists;

public class ChecklistTemplateItemConfiguration : IEntityTypeConfiguration<ChecklistTemplateItem>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplateItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => new { i.TemplateVersionId, i.SortOrder });

        builder.Property(i => i.Label).HasMaxLength(500).IsRequired();
        builder.Property(i => i.HelpText).HasMaxLength(1000);
        builder.Property(i => i.SectionGroup).HasMaxLength(200);
        builder.Property(i => i.DropdownOptions).HasMaxLength(4000);
    }
}
