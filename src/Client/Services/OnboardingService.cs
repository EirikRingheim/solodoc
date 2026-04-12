using System.Net.Http.Json;
using Blazored.LocalStorage;
using Solodoc.Shared.Onboarding;

namespace Solodoc.Client.Services;

public class OnboardingService(ApiHttpClient api, ILocalStorageService localStorage)
{
    public async Task<OnboardingStatusDto?> GetStatusAsync()
    {
        var r = await api.GetAsync("api/onboarding/status");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<OnboardingStatusDto>() : null;
    }

    public async Task<Guid?> CreateTenantAsync(CreateTenantOnboardingRequest request)
    {
        var r = await api.PostAsJsonAsync("api/onboarding/create-tenant", request);
        if (r.IsSuccessStatusCode)
        {
            var result = await r.Content.ReadFromJsonAsync<TenantCreatedResult>();
            if (result is not null)
            {
                // Store new tokens with tenant-admin role
                if (!string.IsNullOrEmpty(result.AccessToken))
                {
                    await localStorage.SetItemAsStringAsync("accessToken", result.AccessToken);
                    await localStorage.SetItemAsStringAsync("refreshToken", result.RefreshToken ?? "");
                    await localStorage.SetItemAsStringAsync("selectedTenantId", result.TenantId.ToString());
                }
                return result.TenantId;
            }
        }
        return null;
    }

    private record TenantCreatedResult(Guid TenantId, string? AccessToken = null, string? RefreshToken = null);

    public async Task<bool> SaveStep1Async(SaveOnboardingStep1Request request)
        => (await api.PostAsJsonAsync("api/onboarding/step1", request)).IsSuccessStatusCode;

    public async Task<bool> SaveStep2Async(SaveOnboardingStep2Request request)
        => (await api.PostAsJsonAsync("api/onboarding/step2", request)).IsSuccessStatusCode;

    public async Task<bool> SaveStep3Async(SaveOnboardingStep3Request request)
        => (await api.PostAsJsonAsync("api/onboarding/step3", request)).IsSuccessStatusCode;

    public async Task<bool> CompleteAsync()
        => (await api.PostAsJsonAsync("api/onboarding/complete", new { })).IsSuccessStatusCode;

    public async Task<bool> ResetAsync()
        => (await api.PostAsJsonAsync("api/onboarding/reset", new { })).IsSuccessStatusCode;
}
