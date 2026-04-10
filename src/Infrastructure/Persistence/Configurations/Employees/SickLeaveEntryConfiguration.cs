using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Employees;

namespace Solodoc.Infrastructure.Persistence.Configurations.Employees;

public class SickLeaveEntryConfiguration : IEntityTypeConfiguration<SickLeaveEntry>
{
    public void Configure(EntityTypeBuilder<SickLeaveEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.PersonId, e.StartDate });
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.Days).HasPrecision(5, 2);
        builder.Property(e => e.Notes).HasMaxLength(2000);
    }
}
