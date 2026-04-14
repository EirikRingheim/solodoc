using Solodoc.Shared.Deviations;

namespace Solodoc.Shared.Projects;

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    string? ClientName,
    DateOnly? StartDate,
    string? Address,
    List<DeviationListItemDto> OpenDeviations,
    Guid? ParentProjectId = null,
    string? ParentProjectName = null,
    List<SubProjectSummaryDto>? SubProjects = null);

public record SubProjectSummaryDto(
    Guid Id,
    string Name,
    string Status,
    decimal TotalHours,
    int OpenDeviations,
    int ChecklistsCompleted,
    int ChecklistsTotal);
