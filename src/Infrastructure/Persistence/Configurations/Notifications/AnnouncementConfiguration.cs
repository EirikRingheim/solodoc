using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Notifications;

namespace Solodoc.Infrastructure.Persistence.Configurations.Notifications;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.CreatedById);

        builder.Property(a => a.Title).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Body).HasMaxLength(8000).IsRequired();

        builder.HasMany(a => a.Acknowledgments)
            .WithOne(ack => ack.Announcement)
            .HasForeignKey(ack => ack.AnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
