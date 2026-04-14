using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Solodoc.Domain.Entities.Expenses;

namespace Solodoc.Infrastructure.Persistence.Configurations.Expenses;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.PersonId, e.Date });
        builder.HasIndex(e => new { e.TenantId, e.Status });

        builder.Property(e => e.Amount).HasPrecision(10, 2);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.ReceiptFileKey).HasMaxLength(500);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);
    }
}

public class TravelExpenseConfiguration : IEntityTypeConfiguration<TravelExpense>
{
    public void Configure(EntityTypeBuilder<TravelExpense> builder)
    {
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.PersonId, e.DepartureDate });
        builder.HasIndex(e => new { e.TenantId, e.Status });

        builder.Property(e => e.Destination).HasMaxLength(500);
        builder.Property(e => e.Purpose).HasMaxLength(500);
        builder.Property(e => e.Route).HasMaxLength(1000);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);

        builder.Property(e => e.TotalKm).HasPrecision(10, 2);
        builder.Property(e => e.DietAmount).HasPrecision(10, 2);
        builder.Property(e => e.MileageAmount).HasPrecision(10, 2);
        builder.Property(e => e.AccommodationAmount).HasPrecision(10, 2);
        builder.Property(e => e.TotalAmount).HasPrecision(10, 2);
        builder.Property(e => e.DocumentedAccommodationCost).HasPrecision(10, 2);

        builder.HasMany(e => e.Days)
            .WithOne(d => d.TravelExpense)
            .HasForeignKey(d => d.TravelExpenseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TravelExpenseDayConfiguration : IEntityTypeConfiguration<TravelExpenseDay>
{
    public void Configure(EntityTypeBuilder<TravelExpenseDay> builder)
    {
        builder.Property(d => d.DietAmount).HasPrecision(10, 2);
    }
}

public class TravelExpenseRateConfiguration : IEntityTypeConfiguration<TravelExpenseRate>
{
    public void Configure(EntityTypeBuilder<TravelExpenseRate> builder)
    {
        builder.HasIndex(r => new { r.TenantId, r.Year }).IsUnique();

        builder.Property(r => r.Diet6To12Hours).HasPrecision(10, 2);
        builder.Property(r => r.Diet12PlusHours).HasPrecision(10, 2);
        builder.Property(r => r.DietOvernight).HasPrecision(10, 2);
        builder.Property(r => r.BreakfastDeductionPct).HasPrecision(5, 2);
        builder.Property(r => r.LunchDeductionPct).HasPrecision(5, 2);
        builder.Property(r => r.DinnerDeductionPct).HasPrecision(5, 2);
        builder.Property(r => r.MileagePerKm).HasPrecision(10, 2);
        builder.Property(r => r.PassengerSurchargePerKm).HasPrecision(10, 2);
        builder.Property(r => r.ForestRoadSurchargePerKm).HasPrecision(10, 2);
        builder.Property(r => r.TrailerSurchargePerKm).HasPrecision(10, 2);
        builder.Property(r => r.UndocumentedNightRate).HasPrecision(10, 2);
    }
}

public class ExpenseSettingsConfiguration : IEntityTypeConfiguration<ExpenseSettings>
{
    public void Configure(EntityTypeBuilder<ExpenseSettings> builder)
    {
        builder.HasIndex(s => s.TenantId).IsUnique();
    }
}
