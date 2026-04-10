namespace Solodoc.Shared.Reports;

public record HoursReportDto(
    decimal TotalHours,
    decimal TotalOvertimeHours,
    decimal AveragePerDay,
    List<CategoryBreakdown> ByCategory,
    List<ProjectBreakdown> ByProject,
    List<DailyHours> ByDay,
    List<EmployeeHours> ByEmployee);

public record CategoryBreakdown(string Category, decimal Hours);
public record ProjectBreakdown(string ProjectName, decimal Hours, decimal OvertimeHours);
public record DailyHours(DateOnly Date, decimal Hours, decimal OvertimeHours);
public record EmployeeHours(string EmployeeName, decimal Hours, decimal OvertimeHours);

public record DeviationReportDto(
    int Total,
    int Open,
    int InProgress,
    int Closed,
    List<SeverityBreakdown> BySeverity,
    List<ProjectDeviations> ByProject,
    List<MonthlyDeviations> ByMonth,
    decimal AverageDaysToClose);

public record SeverityBreakdown(string Severity, int Count);
public record ProjectDeviations(string ProjectName, int Open, int InProgress, int Closed);
public record MonthlyDeviations(string Month, int Count);

public record CertificationReportDto(
    int TotalCertifications,
    int ExpiredCount,
    int ExpiringSoon30,
    int ExpiringSoon60,
    int ExpiringSoon90,
    List<CertTypeBreakdown> ByType,
    List<EmployeeCertStatus> EmployeeStatuses);

public record CertTypeBreakdown(string Type, int Total, int Expired, int ExpiringSoon);
public record EmployeeCertStatus(string EmployeeName, int Total, int Expired, int ExpiringSoon);

public record SafetyReportDto(
    int SjaCount,
    int SafetyRoundsCompleted,
    int HmsMeetingsHeld,
    int IncidentReports,
    List<MonthlyDeviations> DeviationsByMonth);

public record ProjectReportSummaryDto(
    string ProjectName,
    string Status,
    decimal TotalHours,
    int TotalDeviations,
    int OpenDeviations,
    int ChecklistsCompleted,
    int ChecklistsTotal,
    int CrewCount,
    DateOnly? StartDate,
    DateOnly? PlannedEndDate);
