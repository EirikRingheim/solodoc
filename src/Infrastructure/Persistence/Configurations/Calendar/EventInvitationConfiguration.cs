using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Calendar;

namespace Solodoc.Infrastructure.Persistence.Configurations.Calendar;

public class EventInvitationConfiguration : IEntityTypeConfiguration<EventInvitation>
{
    public void Configure(EntityTypeBuilder<EventInvitation> builder)
    {
        builder.HasKey(i => i.Id);
        builder.HasIndex(i => i.EventId);
        builder.HasIndex(i => i.PersonId);

        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
    }
}
