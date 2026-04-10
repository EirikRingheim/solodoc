using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class WorkScheduleDayConfiguration : IEntityTypeConfiguration<WorkScheduleDay>
{
    public void Configure(EntityTypeBuilder<WorkScheduleDay> builder)
    {
        builder.HasKey(d => d.Id);
        builder.HasIndex(d => new { d.WorkScheduleId, d.DayOfWeek }).IsUnique();
    }
}
