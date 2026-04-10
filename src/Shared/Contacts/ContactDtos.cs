namespace Solodoc.Shared.Contacts;

public record ContactListItemDto(
    Guid Id,
    string Name,
    string Type,
    string? Phone,
    string? Email,
    string? City);

public record ContactDetailDto(
    Guid Id,
    string Name,
    string Type,
    string? OrgNumber,
    string? Address,
    string? PostalCode,
    string? City,
    string? Phone,
    string? Email,
    string? Title,
    string? Description,
    string? Notes);

public record CreateContactRequest(
    string Name,
    string Type,
    string? OrgNumber,
    string? Address,
    string? PostalCode,
    string? City,
    string? Phone,
    string? Email,
    string? Title,
    string? Description,
    string? Notes);
