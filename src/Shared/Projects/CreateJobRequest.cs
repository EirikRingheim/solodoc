namespace Solodoc.Shared.Projects;

public record CreateJobRequest(
    string Description,
    Guid? CustomerId,
    string? Address,
    string? Notes);
