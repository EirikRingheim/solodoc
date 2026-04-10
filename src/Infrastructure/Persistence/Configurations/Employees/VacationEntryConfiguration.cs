using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Employees;

namespace Solodoc.Infrastructure.Persistence.Configurations.Employees;

public class VacationEntryConfiguration : IEntityTypeConfiguration<VacationEntry>
{
    public void Configure(EntityTypeBuilder<VacationEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.PersonId, e.StartDate });
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.Days).HasPrecision(5, 2);
        builder.Property(e => e.ApprovedById).HasMaxLength(200);
        builder.Property(e => e.RejectionReason).HasMaxLength(1000);
    }
}
