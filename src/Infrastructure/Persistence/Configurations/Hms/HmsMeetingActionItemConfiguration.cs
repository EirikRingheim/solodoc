using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hms;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hms;

public class HmsMeetingActionItemConfiguration : IEntityTypeConfiguration<HmsMeetingActionItem>
{
    public void Configure(EntityTypeBuilder<HmsMeetingActionItem> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.MeetingId);
        builder.HasIndex(a => a.AssignedToId);

        builder.Property(a => a.Description).HasMaxLength(2000).IsRequired();
    }
}
