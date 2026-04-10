using Solodoc.Application.Common;
using Solodoc.Shared.Auth;

namespace Solodoc.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct);
    Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct);
}
