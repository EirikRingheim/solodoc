using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hms;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hms;

public class HmsMeetingMinutesConfiguration : IEntityTypeConfiguration<HmsMeetingMinutes>
{
    public void Configure(EntityTypeBuilder<HmsMeetingMinutes> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.MeetingId);

        builder.Property(m => m.Content).HasMaxLength(8000).IsRequired();
        builder.Property(m => m.FileKey).HasMaxLength(500);
    }
}
