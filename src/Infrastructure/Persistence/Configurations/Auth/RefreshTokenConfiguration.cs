using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Persistence.Configurations.Auth;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.Token).IsUnique();

        builder.Property(r => r.Token).HasMaxLength(256).IsRequired();
        builder.Property(r => r.ReplacedByToken).HasMaxLength(256);

        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsActive);
    }
}
