using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class AllowanceGroupRuleConfiguration : IEntityTypeConfiguration<AllowanceGroupRule>
{
    public void Configure(EntityTypeBuilder<AllowanceGroupRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.AllowanceGroupId, r.AllowanceRuleId }).IsUnique();

        builder.HasOne(r => r.AllowanceRule)
            .WithMany()
            .HasForeignKey(r => r.AllowanceRuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
