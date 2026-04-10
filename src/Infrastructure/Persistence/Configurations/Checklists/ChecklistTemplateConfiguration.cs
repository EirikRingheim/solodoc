using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Checklists;

namespace Solodoc.Infrastructure.Persistence.Configurations.Checklists;

public class ChecklistTemplateConfiguration : IEntityTypeConfiguration<ChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => new { t.TenantId, t.IsPublished });

        builder.Property(t => t.Name).HasMaxLength(300).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);
        builder.Property(t => t.Tags).HasMaxLength(2000);
    }
}
