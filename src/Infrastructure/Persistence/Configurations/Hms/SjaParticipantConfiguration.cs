using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hms;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hms;

public class SjaParticipantConfiguration : IEntityTypeConfiguration<SjaParticipant>
{
    public void Configure(EntityTypeBuilder<SjaParticipant> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.SjaFormId);
        builder.HasIndex(p => p.PersonId);

        builder.Property(p => p.SignatureFileKey).HasMaxLength(500);
    }
}
