using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Expenses;

public class TravelExpense : TenantScopedEntity
{
    public Guid PersonId { get; set; }
    public Guid? ProjectId { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public TransportMethod TransportMethod { get; set; }
    public string? Route { get; set; }

    // Mileage
    public decimal? TotalKm { get; set; }
    public int Passengers { get; set; }
    public bool ForestRoads { get; set; }
    public bool WithTrailer { get; set; }

    // Calculated amounts
    public decimal DietAmount { get; set; }
    public decimal MileageAmount { get; set; }
    public decimal AccommodationAmount { get; set; }
    public decimal TotalAmount { get; set; }

    // Accommodation
    public int NightsUndocumented { get; set; }
    public decimal DocumentedAccommodationCost { get; set; }

    // Status & approval
    public ExpenseStatus Status { get; set; } = ExpenseStatus.Draft;
    public Guid? ApprovedById { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? PaidById { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    // Navigation
    public ICollection<TravelExpenseDay> Days { get; set; } = [];
}

public class TravelExpenseDay : BaseEntity
{
    public Guid TravelExpenseId { get; set; }
    public DateOnly Date { get; set; }
    public bool BreakfastProvided { get; set; }
    public bool LunchProvided { get; set; }
    public bool DinnerProvided { get; set; }
    public bool IsOvernight { get; set; }
    public decimal DietAmount { get; set; }

    public TravelExpense TravelExpense { get; set; } = null!;
}
