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
    List<DeviationListItemDto> OpenDeviations);
