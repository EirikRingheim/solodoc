namespace Solodoc.Shared.Hours;

public record AbsenceListItemDto(
    Guid Id,
    string Type,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    string? Notes,
    string Status,
    string? EmployeeName);

public record CreateAbsenceRequest(
    string Type,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Hours,
    string? Notes);

// Used in heatmap cells when an absence covers that day
public record AbsenceInfoDto(
    string Type,
    decimal Hours);

public record BalanceSummaryDto(
    decimal VacationAllowanceHours,
    decimal VacationUsedHours,
    decimal VacationRemainingHours,
    decimal SickLeaveHours,
    decimal OvertimeBankBalance,
    bool TimebankEnabled);
