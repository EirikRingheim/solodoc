namespace Solodoc.Shared.Auth;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string FullName, string Email, string Password, string ConfirmPassword);

public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, PersonDto Person);

public record RefreshTokenRequest(string RefreshToken);

public record LogoutRequest(string RefreshToken);

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);

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

// ── Subcontractor invite ────────────────────────────

public record InviteSubcontractorRequest(
    string Email,
    Guid ProjectId,
    int AccessDays,         // 1, 7, 30, 90, 365, 0=unlimited
    bool HoursEnabled);

public record SubcontractorInviteDto(
    Guid InvitationId,
    string Email,
    string ProjectName,
    string TenantName,
    int AccessDays,
    string Status,
    DateTimeOffset ExpiresAt,
    string InviteUrl);

public record SubcontractorAccessDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string? PersonEmail,
    string? PersonCompany,
    Guid ProjectId,
    string ProjectName,
    string State,
    bool HoursEnabled,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);
