namespace Solodoc.Shared.Deviations;

public record UpdateDeviationRequest(
    string Title,
    string? Description,
    string Severity,
    string? Type,
    Guid? CategoryId,
    Guid? ProjectId);
