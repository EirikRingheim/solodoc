namespace Solodoc.Shared.Projects;

public record UpdateProjectRequest(
    string Name,
    string? Description,
    Guid? CustomerId,
    string? ClientName,
    string? Address,
    DateOnly? StartDate,
    DateOnly? PlannedEndDate,
    decimal? EstimatedHours);
