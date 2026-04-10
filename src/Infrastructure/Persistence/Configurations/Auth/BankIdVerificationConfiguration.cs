using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Persistence.Configurations.Auth;

public class BankIdVerificationConfiguration : IEntityTypeConfiguration<BankIdVerification>
{
    public void Configure(EntityTypeBuilder<BankIdVerification> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.PersonalIdHash).HasMaxLength(128).IsRequired();
        builder.Property(b => b.OrgNumber).HasMaxLength(20).IsRequired();
        builder.Property(b => b.VerificationType).HasMaxLength(50).IsRequired();
    }
}
