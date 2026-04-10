using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hms;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hms;

public class SafetyRoundScheduleConfiguration : IEntityTypeConfiguration<SafetyRoundSchedule>
{
    public void Configure(EntityTypeBuilder<SafetyRoundSchedule> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => s.ProjectId);

        builder.Property(s => s.Name).HasMaxLength(300).IsRequired();
    }
}
