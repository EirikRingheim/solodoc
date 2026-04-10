using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> builder)
    {
        builder.HasKey(w => w.Id);
        builder.HasIndex(w => w.TenantId);

        builder.Property(w => w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w => w.WeeklyHours).HasPrecision(5, 2);

        builder.HasMany(w => w.Days)
            .WithOne(d => d.WorkSchedule)
            .HasForeignKey(d => d.WorkScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
