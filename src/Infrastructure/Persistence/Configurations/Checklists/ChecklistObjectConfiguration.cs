using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Checklists;

namespace Solodoc.Infrastructure.Persistence.Configurations.Checklists;

public class ChecklistObjectConfiguration : IEntityTypeConfiguration<ChecklistObject>
{
    public void Configure(EntityTypeBuilder<ChecklistObject> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.ProjectId);
        builder.HasIndex(o => new { o.ProjectId, o.Name, o.Number });

        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();

        builder.Ignore(o => o.DisplayName);
    }
}

public class ChecklistObjectTemplateConfiguration : IEntityTypeConfiguration<ChecklistObjectTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistObjectTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.ChecklistObjectId);

        builder.HasOne(t => t.Object)
            .WithMany(o => o.Templates)
            .HasForeignKey(t => t.ChecklistObjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
