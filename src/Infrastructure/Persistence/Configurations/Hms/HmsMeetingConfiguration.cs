using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hms;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hms;

public class HmsMeetingConfiguration : IEntityTypeConfiguration<HmsMeeting>
{
    public void Configure(EntityTypeBuilder<HmsMeeting> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => m.CreatedById);

        builder.Property(m => m.Title).HasMaxLength(300).IsRequired();
        builder.Property(m => m.Location).HasMaxLength(500);

        builder.HasMany(m => m.Minutes)
            .WithOne(mm => mm.Meeting)
            .HasForeignKey(mm => mm.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.ActionItems)
            .WithOne(a => a.Meeting)
            .HasForeignKey(a => a.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
