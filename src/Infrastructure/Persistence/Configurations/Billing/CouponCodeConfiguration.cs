using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Billing;

namespace Solodoc.Infrastructure.Persistence.Configurations.Billing;

public class CouponCodeConfiguration : IEntityTypeConfiguration<CouponCode>
{
    public void Configure(EntityTypeBuilder<CouponCode> builder)
    {
        builder.HasIndex(c => c.Code).IsUnique();
        builder.Property(c => c.Code).HasMaxLength(50);
        builder.Property(c => c.Description).HasMaxLength(500);
    }
}

public class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.HasIndex(r => new { r.TenantId, r.CouponCodeId }).IsUnique();
        builder.HasOne(r => r.CouponCode)
            .WithMany(c => c.Redemptions)
            .HasForeignKey(r => r.CouponCodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
