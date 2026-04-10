namespace Solodoc.Shared.Projects;

public record JobDetailDto(
    Guid Id,
    string Description,
    string Status,
    string? CustomerName,
    string? Address,
    string? Notes,
    string CreatedBy,
    DateTimeOffset CreatedAt,
    List<JobPartsItemDto> PartsItems);
