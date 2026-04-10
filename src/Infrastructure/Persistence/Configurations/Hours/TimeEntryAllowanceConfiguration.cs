using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class TimeEntryAllowanceConfiguration : IEntityTypeConfiguration<TimeEntryAllowance>
{
    public void Configure(EntityTypeBuilder<TimeEntryAllowance> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.TimeEntryId, a.AllowanceRuleId });

        builder.Property(a => a.Hours).HasPrecision(5, 2);
        builder.Property(a => a.Amount).HasPrecision(10, 2);
        builder.Property(a => a.Notes).HasMaxLength(500);

        builder.HasOne(a => a.AllowanceRule)
            .WithMany()
            .HasForeignKey(a => a.AllowanceRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
