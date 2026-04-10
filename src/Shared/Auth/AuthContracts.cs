namespace Solodoc.Shared.Auth;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string FullName, string Email, string Password, string ConfirmPassword);

public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, PersonDto Person);

public record RefreshTokenRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record PersonDto(Guid Id, string Email, string FullName);

public record InvitationDetailDto(
    Guid Id,
    string Email,
    string Status,
    DateTimeOffset ExpiresAt,
    string InvitedByName,
    string TenantName,
    string Role);

public record AcceptInvitationRequest(
    string? FullName,
    string? Password);

public record AcceptInvitationResponse(
    string Email,
    Guid TenantId,
    AuthResponse Auth);
