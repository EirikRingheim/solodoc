using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Application.Auth;

public interface ITokenService
{
    string GenerateAccessToken(Person person, Guid? tenantId = null, string? role = null);
    string GenerateRefreshToken();
}
