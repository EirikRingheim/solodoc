namespace Solodoc.Shared.Projects;

public record CustomerDto(
    Guid Id,
    string Name,
    string Type,
    string? OrgNumber,
    string? Address,
    string? City,
    string? Phone,
    string? Email);
