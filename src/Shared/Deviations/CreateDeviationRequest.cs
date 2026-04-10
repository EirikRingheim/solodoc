namespace Solodoc.Shared.Deviations;

public record CreateDeviationRequest(
    string Title,
    string? Description,
    string Severity,
    Guid? ProjectId,
    Guid? JobId = null,
    Guid? LocationId = null,
    Guid? CategoryId = null);
