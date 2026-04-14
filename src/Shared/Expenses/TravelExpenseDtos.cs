namespace Solodoc.Shared.Expenses;

// ── Travel Expense (Reiseregning) ────────────────────

public record TravelExpenseListItemDto(
    Guid Id, string EmployeeName, Guid PersonId,
    DateOnly DepartureDate, DateOnly ReturnDate,
    string Destination, string Purpose,
    decimal TotalAmount, string Status,
    bool IsApproved, bool IsPaid,
    DateTimeOffset CreatedAt);

public record TravelExpenseDetailDto(
    Guid Id, Guid PersonId, string EmployeeName,
    DateOnly DepartureDate, DateOnly ReturnDate,
    string Destination, string Purpose,
    string TransportMethod, string? Route,
    decimal? TotalKm, int Passengers,
    bool ForestRoads, bool WithTrailer,
    decimal DietAmount, decimal MileageAmount,
    decimal AccommodationAmount, decimal TotalAmount,
    int NightsUndocumented, decimal DocumentedAccommodationCost,
    string Status, string? ApprovedByName, DateTimeOffset? ApprovedAt,
    string? PaidByName, DateTimeOffset? PaidAt,
    string? RejectionReason,
    List<TravelExpenseDayDto> Days);

public record TravelExpenseDayDto(
    DateOnly Date, bool BreakfastProvided,
    bool LunchProvided, bool DinnerProvided,
    bool IsOvernight, decimal DietAmount);

public record CreateTravelExpenseRequest(
    Guid? ProjectId,
    DateOnly DepartureDate, DateOnly ReturnDate,
    string Destination, string Purpose,
    string TransportMethod, string? Route,
    decimal? TotalKm, int Passengers,
    bool ForestRoads, bool WithTrailer,
    int NightsUndocumented,
    decimal DocumentedAccommodationCost,
    List<CreateTravelExpenseDayRequest> Days);

public record CreateTravelExpenseDayRequest(
    DateOnly Date, bool BreakfastProvided,
    bool LunchProvided, bool DinnerProvided,
    bool IsOvernight);

// ── Calculation preview ──────────────────────────────

public record TravelExpenseCalculationDto(
    decimal DietTotal, decimal MileageTotal,
    decimal AccommodationTotal, decimal GrandTotal,
    List<TravelExpenseDayDto> DayBreakdown);

// ── Rates ────────────────────────────────────────────

public record TravelExpenseRateDto(
    Guid Id, int Year,
    decimal Diet6To12Hours, decimal Diet12PlusHours,
    decimal DietOvernight,
    decimal BreakfastDeductionPct, decimal LunchDeductionPct,
    decimal DinnerDeductionPct,
    decimal MileagePerKm, decimal PassengerSurchargePerKm,
    decimal ForestRoadSurchargePerKm, decimal TrailerSurchargePerKm,
    decimal UndocumentedNightRate,
    bool IsActive);

public record CreateTravelExpenseRateRequest(
    int Year,
    decimal Diet6To12Hours, decimal Diet12PlusHours,
    decimal DietOvernight,
    decimal BreakfastDeductionPct, decimal LunchDeductionPct,
    decimal DinnerDeductionPct,
    decimal MileagePerKm, decimal PassengerSurchargePerKm,
    decimal ForestRoadSurchargePerKm, decimal TrailerSurchargePerKm,
    decimal UndocumentedNightRate);
