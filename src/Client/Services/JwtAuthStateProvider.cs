using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace Solodoc.Client.Services;

public class JwtAuthStateProvider(ILocalStorageService localStorage, HttpClient http) : AuthenticationStateProvider
{
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await localStorage.GetItemAsStringAsync("accessToken");

        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(_anonymous);

        token = token.Trim('"');
        var claims = ParseClaimsFromJwt(token);

        // Token is valid — use it
        if (claims is not null)
        {
            var identity = new ClaimsIdentity(claims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        // Access token expired — try refresh
        var refreshedClaims = await TryRefreshTokenAsync();
        if (refreshedClaims is not null)
        {
            var identity = new ClaimsIdentity(refreshedClaims, "jwt");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        return new AuthenticationState(_anonymous);
    }

    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private async Task<IEnumerable<Claim>?> TryRefreshTokenAsync()
    {
        try
        {
            var refreshToken = await localStorage.GetItemAsStringAsync("refreshToken");
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            refreshToken = refreshToken.Trim('"');

            var response = await http.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = refreshToken });
            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<RefreshResult>();
            if (result is null)
                return null;

            await localStorage.SetItemAsStringAsync("accessToken", result.AccessToken);
            await localStorage.SetItemAsStringAsync("refreshToken", result.RefreshToken);

            return ParseClaimsFromJwt(result.AccessToken);
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<Claim>? ParseClaimsFromJwt(string jwt)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            // Truly expired (with 2 min tolerance for clock skew)
            if (token.ValidTo < DateTime.UtcNow.AddMinutes(-2))
                return null;

            // Still valid — return claims (refresh happens on API 401)
            return token.Claims;
        }
        catch
        {
            return null;
        }
    }

    private record RefreshResult(string AccessToken, string RefreshToken);
}
