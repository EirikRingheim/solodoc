using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Expenses;

public class TravelExpenseRate : TenantScopedEntity
{
    public int Year { get; set; }

    // Diet rates (diettgodtgjørelse)
    public decimal Diet6To12Hours { get; set; } = 397m;
    public decimal Diet12PlusHours { get; set; } = 736m;
    public decimal DietOvernight { get; set; } = 1012m;

    // Meal deduction percentages
    public decimal BreakfastDeductionPct { get; set; } = 20m;
    public decimal LunchDeductionPct { get; set; } = 30m;
    public decimal DinnerDeductionPct { get; set; } = 50m;

    // Mileage (kilometergodtgjørelse)
    public decimal MileagePerKm { get; set; } = 5.30m;
    public decimal PassengerSurchargePerKm { get; set; } = 1.00m;
    public decimal ForestRoadSurchargePerKm { get; set; } = 1.00m;
    public decimal TrailerSurchargePerKm { get; set; } = 1.00m;

    // Overnight (nattillegg)
    public decimal UndocumentedNightRate { get; set; } = 452m;

    public bool IsActive { get; set; } = true;
}
