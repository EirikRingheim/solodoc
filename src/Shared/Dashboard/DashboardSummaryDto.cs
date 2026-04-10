namespace Solodoc.Shared.Dashboard;

public record DashboardSummaryDto(
    int ActiveProjects,
    decimal HoursThisMonth,
    int OpenDeviations,
    List<DashboardProjectDto> RecentProjects,
    List<DashboardDeviationDto> OpenDeviationsList,
    decimal HoursThisWeek = 0m);

public record DashboardProjectDto(
    Guid Id,
    string Name,
    string Status);

public record DashboardDeviationDto(
    Guid Id,
    string Title,
    string Status,
    string? ProjectName);
