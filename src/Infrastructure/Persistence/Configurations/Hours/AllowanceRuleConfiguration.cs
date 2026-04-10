using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class AllowanceRuleConfiguration : IEntityTypeConfiguration<AllowanceRule>
{
    public void Configure(EntityTypeBuilder<AllowanceRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => r.TenantId);

        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Amount).HasPrecision(10, 2);
        builder.Property(r => r.ApplicableDays).HasColumnType("jsonb");
    }
}
