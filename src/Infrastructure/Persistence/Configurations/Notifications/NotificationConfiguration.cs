using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Notifications;

namespace Solodoc.Infrastructure.Persistence.Configurations.Notifications;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.HasIndex(n => n.PersonId);
        builder.HasIndex(n => n.TenantId);
        builder.HasIndex(n => new { n.PersonId, n.IsRead });

        builder.Property(n => n.Title).HasMaxLength(300).IsRequired();
        builder.Property(n => n.Message).HasMaxLength(2000);
        builder.Property(n => n.LinkUrl).HasMaxLength(500);
        builder.Property(n => n.Type).HasMaxLength(100);
    }
}
