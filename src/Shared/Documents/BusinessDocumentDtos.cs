namespace Solodoc.Shared.Documents;

public record BusinessDocumentDto(
    Guid Id,
    string DocumentType,
    string Title,
    Guid? ProjectId,
    string? ProjectName,
    string Status,
    DateTimeOffset CreatedAt,
    string? GeneratedPdfKey);

public record CreateBusinessDocumentRequest(
    string DocumentType,
    string Title,
    Guid? ProjectId,
    string? ContentJson);

public record WasteDisposalEntryDto(
    Guid Id,
    string Category,
    string? Description,
    decimal? WeightKg,
    DateOnly DisposedAt,
    string? DisposalMethod,
    string? ReceiptFileKey);

public record CreateWasteDisposalEntryRequest(
    string Category,
    string? Description,
    decimal? WeightKg,
    DateOnly DisposedAt,
    string? DisposalMethod);

public record ChangeOrderContentDto(
    string Description,
    string Reason,
    decimal? CostImpact,
    string? CostType,
    int? ScheduleImpactDays,
    string? ScheduleType);
