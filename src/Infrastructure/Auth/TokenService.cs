using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Solodoc.Application.Auth;
using Solodoc.Domain.Entities.Auth;

namespace Solodoc.Infrastructure.Auth;

public class TokenService(IConfiguration configuration) : ITokenService
{
    public string GenerateAccessToken(Person person, Guid? tenantId = null, string? role = null)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, person.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, person.Email),
            new("fullName", person.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (tenantId.HasValue)
            claims.Add(new Claim("tenantId", tenantId.Value.ToString()));

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        if (person.SystemRole is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, person.SystemRole.Value.ToString()));
            claims.Add(new Claim("role", person.SystemRole.Value.ToString()));
            // Dedicated non-array claim for reliable client-side detection
            claims.Add(new Claim("sa", "1"));
        }

        var accessMinutes = int.Parse(configuration["Jwt:AccessTokenMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(accessMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
