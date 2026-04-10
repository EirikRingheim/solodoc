namespace Solodoc.Shared.Deviations;

public record CloseDeviationRequest(string? CorrectiveAction, DateTimeOffset? CompletedAt);
