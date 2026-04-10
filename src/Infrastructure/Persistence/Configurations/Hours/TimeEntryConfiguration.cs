using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.TenantId);
        builder.HasIndex(t => new { t.PersonId, t.Date });
        builder.HasIndex(t => new { t.TenantId, t.Status });

        builder.Property(t => t.Hours).HasPrecision(5, 2);
        builder.Property(t => t.OvertimeHours).HasPrecision(5, 2);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Notes).HasMaxLength(2000);

        builder.HasMany(t => t.Allowances)
            .WithOne(a => a.TimeEntry)
            .HasForeignKey(a => a.TimeEntryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
