namespace Solodoc.Shared.Equipment;

public record EquipmentListItemDto(
    Guid Id,
    string Name,
    string? Type,
    string? RegistrationNumber,
    string? Make,
    string? Model,
    bool IsActive,
    string? CurrentProjectName = null,
    string? LocationDescription = null);

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
    double? Latitude,
    double? Longitude,
    string? LocationDescription,
    Guid? CurrentProjectId,
    string? CurrentProjectName,
    List<MaintenanceLogDto> MaintenanceLog,
    List<EquipmentAssignmentDto> Assignments);

public record MaintenanceLogDto(
    Guid Id,
    string Description,
    DateOnly Date,
    string? PerformedBy,
    decimal? Cost);

public record EquipmentAssignmentDto(
    Guid Id,
    Guid ProjectId,
    string ProjectName,
    DateOnly AssignedFrom,
    DateOnly? AssignedTo);

public record CreateEquipmentRequest(
    string Name,
    string? Type,
    string? RegistrationNumber,
    string? SerialNumber,
    int? Year,
    string? Make,
    string? Model);

public record UpdateEquipmentLocationRequest(
    Guid? CurrentProjectId,
    Guid? CurrentJobId,
    Guid? CurrentLocationId,
    double? Latitude,
    double? Longitude,
    string? LocationDescription);

public record AssignEquipmentToProjectRequest(
    Guid ProjectId,
    DateOnly AssignedFrom,
    DateOnly? AssignedTo);

public record AddMaintenanceRequest(
    string Description,
    DateOnly Date,
    decimal? Cost,
    string? Notes);

// ── Equipment Type Categories ──

public record EquipmentTypeCategoryDto(
    Guid Id,
    string Name,
    bool IsActive,
    int SortOrder,
    bool IsDefault);

public record CreateEquipmentTypeCategoryRequest(string Name);
