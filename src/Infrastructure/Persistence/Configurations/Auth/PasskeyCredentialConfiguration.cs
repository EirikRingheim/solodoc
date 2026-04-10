using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Persistence.Configurations.Auth;

public class PasskeyCredentialConfiguration : IEntityTypeConfiguration<PasskeyCredential>
{
    public void Configure(EntityTypeBuilder<PasskeyCredential> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => c.CredentialId).IsUnique();

        builder.Property(c => c.CredentialId).IsRequired();
        builder.Property(c => c.PublicKey).IsRequired();
        builder.Property(c => c.DeviceName).HasMaxLength(200);
    }
}
