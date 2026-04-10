using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Hours;

namespace Solodoc.Infrastructure.Persistence.Configurations.Hours;

public class EmployeeScheduleAssignmentConfiguration : IEntityTypeConfiguration<EmployeeScheduleAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeScheduleAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.PersonId, a.EffectiveFrom });

        builder.HasOne(a => a.WorkSchedule)
            .WithMany()
            .HasForeignKey(a => a.WorkScheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
