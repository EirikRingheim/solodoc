using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Notifications;

namespace Solodoc.Infrastructure.Persistence.Configurations.Notifications;

public class AnnouncementDismissalConfiguration : IEntityTypeConfiguration<AnnouncementDismissal>
{
    public void Configure(EntityTypeBuilder<AnnouncementDismissal> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.AnnouncementId, d.PersonId }).IsUnique();
        builder.HasIndex(d => d.PersonId);
    }
}
