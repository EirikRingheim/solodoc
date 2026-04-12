using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Persistence.Configurations.Auth;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasIndex(t => t.OrgNumber).IsUnique().HasFilter("org_number <> ''");

        builder.Property(t => t.Name).HasMaxLength(300).IsRequired();
        builder.Property(t => t.OrgNumber).HasMaxLength(20).IsRequired();
        builder.Property(t => t.BusinessAddress).HasMaxLength(500);
        builder.Property(t => t.LogoFileKey).HasMaxLength(500);
        builder.Property(t => t.AccentColor).HasMaxLength(7);
        builder.Property(t => t.DefaultTimeZoneId).HasMaxLength(100);

        builder.HasMany(t => t.Memberships)
            .WithOne(m => m.Tenant)
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Invitations)
            .WithOne(i => i.Tenant)
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
