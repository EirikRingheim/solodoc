namespace Solodoc.Shared.Projects;

public record JobPartsItemDto(
    Guid Id,
    string Description,
    string Status,
    string? Notes);
