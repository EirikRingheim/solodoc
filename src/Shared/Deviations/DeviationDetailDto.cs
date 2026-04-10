namespace Solodoc.Shared.Deviations;

public record DeviationDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Severity,
    string? Category,
    string? ProjectName,
    Guid? ProjectId,
    string ReportedBy,
    string? AssignedTo,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    string? CorrectiveAction,
    DateTimeOffset? CorrectiveActionDeadline,
    DateTimeOffset? CorrectiveActionCompletedAt,
    List<DeviationCommentDto> Comments);

public record UpdateDeviationStatusRequest(string NewStatus);
