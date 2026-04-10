namespace Solodoc.Shared.Projects;

public record ProjectListItemDto(
    Guid Id,
    string Name,
    string Status,
    string? ClientName,
    DateOnly? StartDate,
    int OpenDeviations);
