using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Solodoc.Application.Auth;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;

namespace Solodoc.Infrastructure.Auth;

public class AuthService(
    SolodocDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IConfiguration configuration) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var exists = await db.Persons.AnyAsync(p => p.Email == request.Email.ToLowerInvariant(), ct);
        if (exists)
            return Result<AuthResponse>.Failure("En konto med denne e-postadressen finnes allerede.");

        var person = new Person
        {
            Email = request.Email.ToLowerInvariant(),
            FullName = request.FullName,
            PasswordHash = passwordHasher.Hash(request.Password),
            State = PersonState.Active,
            EmailVerified = true
        };

        db.Persons.Add(person);
        await db.SaveChangesAsync(ct);

        return await GenerateAuthResponseAsync(person, ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var person = await db.Persons
            .FirstOrDefaultAsync(p => p.Email == request.Email.ToLowerInvariant(), ct);

        if (person is null || !passwordHasher.Verify(request.Password, person.PasswordHash))
            return Result<AuthResponse>.Failure("Feil e-postadresse eller passord.");

        if (person.State == PersonState.Deactivated)
            return Result<AuthResponse>.Failure("Kontoen er deaktivert.");

        return await GenerateAuthResponseAsync(person, ct);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var storedToken = await db.RefreshTokens
            .Include(r => r.Person)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (storedToken is null || !storedToken.IsActive)
            return Result<AuthResponse>.Failure("Ugyldig eller utløpt refresh-token.");

        // Revoke old token
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTimeOffset.UtcNow;

        // Generate new tokens
        var person = storedToken.Person;
        var result = await GenerateAuthResponseAsync(person, ct);

        // Link old → new
        if (result.IsSuccess)
            storedToken.ReplacedByToken = result.Value!.RefreshToken;

        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var storedToken = await db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken, ct);

        if (storedToken is not null && storedToken.IsActive)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<AuthResponse>> GenerateAuthResponseAsync(Person person, CancellationToken ct)
    {
        // Find the user's first active tenant membership for the JWT claim
        var membership = await db.TenantMemberships
            .Where(m => m.PersonId == person.Id && m.State == TenantMembershipState.Active)
            .FirstOrDefaultAsync(ct);

        var tenantId = membership is not null ? (Guid?)membership.TenantId : null;
        var role = membership?.Role switch
        {
            TenantRole.TenantAdmin => "tenant-admin",
            TenantRole.ProjectLeader => "project-leader",
            TenantRole.FieldWorker => "field-worker",
            _ => null
        };

        var accessToken = tokenService.GenerateAccessToken(person, tenantId, role);
        var refreshTokenValue = tokenService.GenerateRefreshToken();

        var refreshDays = int.Parse(configuration["Jwt:RefreshTokenDays"] ?? "7");
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

        // Store refresh token in DB
        var refreshToken = new RefreshToken
        {
            PersonId = person.Id,
            Token = refreshTokenValue,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(refreshDays)
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        var response = new AuthResponse(
            accessToken,
            refreshTokenValue,
            expiresAt,
            new PersonDto(person.Id, person.Email, person.FullName));

        return Result<AuthResponse>.Success(response);
    }
}
