using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Persistence.Configurations.Auth;

public class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.Email).IsUnique();

        builder.Property(p => p.Email).HasMaxLength(256).IsRequired();
        builder.Property(p => p.FullName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.TimeZoneId).HasMaxLength(100);
        builder.Property(p => p.PhoneNumber).HasMaxLength(30);

        builder.HasMany(p => p.TenantMemberships)
            .WithOne(m => m.Person)
            .HasForeignKey(m => m.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.SubcontractorAccesses)
            .WithOne(s => s.Person)
            .HasForeignKey(s => s.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.PasskeyCredentials)
            .WithOne(c => c.Person)
            .HasForeignKey(c => c.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.RefreshTokens)
            .WithOne(r => r.Person)
            .HasForeignKey(r => r.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.BankIdVerifications)
            .WithOne(b => b.Person)
            .HasForeignKey(b => b.PersonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
