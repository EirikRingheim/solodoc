namespace Solodoc.Shared.Projects;

public record UpdateCustomerRequest(
    string Name,
    string Type,
    string? OrgNumber,
    string? Address,
    string? PostalCode,
    string? City,
    string? Phone,
    string? Email,
    string? Notes);
