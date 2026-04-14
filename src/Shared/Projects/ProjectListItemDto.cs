namespace Solodoc.Shared.Projects;

public record ProjectListItemDto(
    Guid Id,
    string Name,
    string Status,
    string? ClientName,
    DateOnly? StartDate,
    int OpenDeviations,
    Guid? ParentProjectId = null,
    string? ParentProjectName = null,
    int SubProjectCount = 0,
    int SubProjectsCompleted = 0);
