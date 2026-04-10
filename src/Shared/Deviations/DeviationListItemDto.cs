namespace Solodoc.Shared.Deviations;

public record DeviationListItemDto(
    Guid Id,
    string Title,
    string? ProjectName,
    string Status,
    string Severity,
    string ReportedBy,
    DateTimeOffset CreatedAt);
