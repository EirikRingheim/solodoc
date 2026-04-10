using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Notifications;

namespace Solodoc.Infrastructure.Persistence.Configurations.Notifications;

public class AnnouncementAcknowledgmentConfiguration : IEntityTypeConfiguration<AnnouncementAcknowledgment>
{
    public void Configure(EntityTypeBuilder<AnnouncementAcknowledgment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.AnnouncementId);
        builder.HasIndex(a => a.PersonId);
    }
}
