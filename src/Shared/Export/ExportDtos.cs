namespace Solodoc.Shared.Export;

public record CreateProjectExportRequest(
    string OutputMode,         // CombinedPdf, StructuredZip, IndividualFiles
    string? PhotoOption,       // full, compressed, thumbnail, none
    bool IncludeDeviations,
    bool IncludeChecklists,
    bool IncludeHours,
    bool IncludeSja);

public record CreateEmployeeExportRequest(
    string OutputMode,
    bool IncludeCertifications,
    bool IncludeTraining,
    bool IncludeHours);

public record CreateCustomExportRequest(
    string OutputMode,
    string? PhotoOption,
    List<ExportItemSelection> Items);

public record ExportItemSelection(
    string Type,    // "project", "deviation", "checklist", "sja", "equipment", "employee"
    Guid Id);

public record ExportJobDto(
    Guid Id,
    string Type,
    string Status,
    string OutputMode,
    int? ProgressPercent,
    string? ResultFileName,
    long? ResultFileSizeBytes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorMessage);
