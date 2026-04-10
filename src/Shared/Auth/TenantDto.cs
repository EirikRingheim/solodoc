namespace Solodoc.Shared.Auth;

public record TenantDto(Guid Id, string Name, string OrgNumber, string Role, string? AccentColor);
