namespace Solodoc.Shared.Chemicals;

public record ChemicalListItemDto(
    Guid Id,
    string Name,
    string? Manufacturer,
    bool IsActive,
    List<string> GhsPictograms);

public record ChemicalDetailDto(
    Guid Id,
    string Name,
    string? Manufacturer,
    string? ProductNumber,
    bool IsActive,
    List<GhsPictogramDto> GhsPictograms,
    List<PpeRequirementDto> PpeRequirements);

public record GhsPictogramDto(
    string Code,
    string? Description);

public record PpeRequirementDto(
    string Requirement,
    string? IconCode);

public record CreateChemicalRequest(
    string Name,
    string? Manufacturer,
    string? ProductNumber);
