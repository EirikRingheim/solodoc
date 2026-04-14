namespace Solodoc.Shared.Projects;

public record CreateProjectRequest(
    string Name,
    string? Description,
    Guid? CustomerId,
    string? ClientName,
    string? Address,
    DateOnly? StartDate,
    DateOnly? PlannedEndDate,
    decimal? EstimatedHours,
    Guid? ParentProjectId = null);
