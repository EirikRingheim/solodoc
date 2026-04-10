using System.Net.Http.Json;
using Blazored.LocalStorage;
using Solodoc.Shared.Auth;

namespace Solodoc.Client.Services;

public class AuthHttpService(
    HttpClient http,
    ILocalStorageService localStorage,
    JwtAuthStateProvider authStateProvider)
{
    public async Task<(bool Success, string? Error)> LoginAsync(LoginRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/login", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Error ?? "Innlogging feilet.");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        await StoreTokens(result!);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(RegisterRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/register", request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (false, error?.Error ?? "Registrering feilet.");
        }

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        await StoreTokens(result!);
        return (true, null);
    }

    public async Task LogoutAsync()
    {
        await localStorage.RemoveItemAsync("accessToken");
        await localStorage.RemoveItemAsync("refreshToken");
        await localStorage.RemoveItemAsync("selectedTenantId");
        authStateProvider.NotifyAuthStateChanged();
    }

    public async Task StoreTokens(AuthResponse result)
    {
        await localStorage.SetItemAsStringAsync("accessToken", result.AccessToken);
        await localStorage.SetItemAsStringAsync("refreshToken", result.RefreshToken);
        authStateProvider.NotifyAuthStateChanged();
    }

    public async Task<List<Solodoc.Shared.Auth.TenantDto>> GetTenantsAsync()
    {
        // Use a separate client with the token for this authenticated call
        var token = await localStorage.GetItemAsStringAsync("accessToken");
        if (string.IsNullOrWhiteSpace(token)) return [];

        token = token.Trim('"');
        var request = new HttpRequestMessage(HttpMethod.Get, "api/auth/tenants");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<Solodoc.Shared.Auth.TenantDto>>() ?? [];
        return [];
    }

    public async Task SelectTenantAsync(Guid tenantId)
    {
        await localStorage.SetItemAsStringAsync("selectedTenantId", tenantId.ToString());
        authStateProvider.NotifyAuthStateChanged();
    }

    public async Task<string?> GetSelectedTenantIdAsync()
    {
        return await localStorage.GetItemAsStringAsync("selectedTenantId");
    }

    private record ErrorResponse(string? Error);
}
