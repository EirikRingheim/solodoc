using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Checklists;

namespace Solodoc.Infrastructure.Persistence.Configurations.Checklists;

public class ChecklistInstanceItemConfiguration : IEntityTypeConfiguration<ChecklistInstanceItem>
{
    public void Configure(EntityTypeBuilder<ChecklistInstanceItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.InstanceId);

        builder.Property(i => i.Value).HasMaxLength(4000);
        builder.Property(i => i.PhotoFileKey).HasMaxLength(500);
        builder.Property(i => i.SignatureFileKey).HasMaxLength(500);
    }
}
