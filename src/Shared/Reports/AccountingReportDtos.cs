namespace Solodoc.Shared.Reports;

public record AccountingReportDto(
    DateOnly PeriodStart, DateOnly PeriodEnd,
    string PeriodType,
    AccountingTotals Totals,
    List<EmployeeAccountingRow> ByEmployee,
    List<ProjectAccountingRow> ByProject);

public record AccountingTotals(
    decimal TotalHours, decimal TotalOvertimeHours,
    decimal TotalExpenses, decimal TotalTravelExpenses,
    decimal TotalAllowances,
    decimal VacationDays, decimal SickLeaveDays,
    decimal GrandTotal);

public record EmployeeAccountingRow(
    Guid PersonId, string EmployeeName,
    decimal Hours, decimal OvertimeHours,
    decimal ExpenseAmount, decimal TravelExpenseAmount,
    decimal AllowanceAmount,
    decimal VacationDays, decimal SickLeaveDays,
    decimal EmployeeTotal);

public record ProjectAccountingRow(
    Guid ProjectId, string ProjectName,
    decimal Hours, decimal OvertimeHours,
    decimal ExpenseAmount, decimal TravelExpenseAmount,
    decimal AllowanceAmount,
    decimal ProjectTotal);
