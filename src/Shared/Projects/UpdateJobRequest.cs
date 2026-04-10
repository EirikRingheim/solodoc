namespace Solodoc.Shared.Projects;

public record UpdateJobRequest(
    string Description,
    Guid? CustomerId,
    string? Address,
    string? Notes);
