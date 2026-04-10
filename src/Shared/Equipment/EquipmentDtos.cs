namespace Solodoc.Shared.Equipment;

public record EquipmentListItemDto(
    Guid Id,
    string Name,
    string? Type,
    string? RegistrationNumber,
    string? Make,
    string? Model,
    bool IsActive);

public record EquipmentDetailDto(
    Guid Id,
    string Name,
    string? Type,
    string? RegistrationNumber,
    string? SerialNumber,
    int? Year,
    string? Make,
    string? Model,
    bool IsActive,
    List<MaintenanceLogDto> MaintenanceLog);

public record MaintenanceLogDto(
    Guid Id,
    string Description,
    DateOnly Date,
    string? PerformedBy,
    decimal? Cost);

public record CreateEquipmentRequest(
    string Name,
    string? Type,
    string? RegistrationNumber,
    string? SerialNumber,
    int? Year,
    string? Make,
    string? Model);

public record AddMaintenanceRequest(
    string Description,
    DateOnly Date,
    decimal? Cost,
    string? Notes);
