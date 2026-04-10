namespace Solodoc.Shared.Locations;

public record LocationListItemDto(
    Guid Id,
    string Name,
    string? Address,
    string? Description,
    string LocationType,
    int AssignedTemplates,
    int CompletedInstances);

public record LocationDetailDto(
    Guid Id,
    string Name,
    string? Address,
    string? Description,
    string LocationType);

public record CreateLocationRequest(
    string Name,
    string? Address,
    string? Description,
    string LocationType);

public record AssignTemplateToLocationRequest(
    Guid TemplateId,
    Guid LocationId);
