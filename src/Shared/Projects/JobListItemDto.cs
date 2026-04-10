namespace Solodoc.Shared.Projects;

public record JobListItemDto(
    Guid Id,
    string Description,
    string Status,
    string? CustomerName,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    decimal Hours);
