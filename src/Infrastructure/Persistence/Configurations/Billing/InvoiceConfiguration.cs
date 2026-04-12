using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Billing;

namespace Solodoc.Infrastructure.Persistence.Configurations.Billing;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.TenantId);
        builder.HasIndex(i => new { i.TenantId, i.Year, i.Month });
        builder.HasIndex(i => i.Status);

        builder.Property(i => i.InvoiceNumber).HasMaxLength(20);
        builder.Property(i => i.CustomerName).HasMaxLength(300);
        builder.Property(i => i.CustomerOrgNumber).HasMaxLength(20);
        builder.Property(i => i.CustomerAddress).HasMaxLength(500);
        builder.Property(i => i.DiscountReason).HasMaxLength(200);
        builder.Property(i => i.CouponCode).HasMaxLength(50);
    }
}
