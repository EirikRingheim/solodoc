using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Employees;

namespace Solodoc.Infrastructure.Persistence.Configurations.Employees;

public class VacationBalanceConfiguration : IEntityTypeConfiguration<VacationBalance>
{
    public void Configure(EntityTypeBuilder<VacationBalance> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.PersonId, e.Year, e.TenantId }).IsUnique();

        builder.Property(e => e.AnnualAllowanceDays).HasPrecision(5, 2);
        builder.Property(e => e.CarriedOverDays).HasPrecision(5, 2);
        builder.Property(e => e.UsedDays).HasPrecision(5, 2);

        builder.Ignore(e => e.RemainingDays);
    }
}
