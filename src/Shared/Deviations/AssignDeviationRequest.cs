namespace Solodoc.Shared.Deviations;

public record AssignDeviationRequest(Guid AssignedToId, DateTimeOffset? Deadline);
