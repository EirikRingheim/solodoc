using System.Net.Http.Json;
using Solodoc.Shared.CheckIn;

namespace Solodoc.Client.Services;

public class CheckInService(ApiHttpClient api)
{
    public async Task<bool> CheckInAsync(CheckInRequest request)
    {
        return (await api.PostAsJsonAsync("api/checkin", request)).IsSuccessStatusCode;
    }

    public async Task<bool> CheckOutAsync(CheckOutRequest request)
    {
        return (await api.PostAsJsonAsync("api/checkout", request)).IsSuccessStatusCode;
    }

    public async Task<MyCheckInStatusDto?> GetMyStatusAsync()
    {
        var r = await api.GetAsync("api/checkin/status");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<MyCheckInStatusDto>() : null;
    }

    public async Task<List<OnSitePersonDto>> GetOnSiteAsync(string siteType, Guid siteId)
    {
        var r = await api.GetAsync($"api/checkin/on-site/{siteType}/{siteId}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<OnSitePersonDto>>() ?? [] : [];
    }

    public async Task<List<SiteOverviewDto>> GetAllSitesAsync()
    {
        var r = await api.GetAsync("api/checkin/all-sites");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<SiteOverviewDto>>() ?? [] : [];
    }

    public async Task<List<CheckInHistoryDto>> GetHistoryAsync(string siteType, Guid siteId, int days = 30)
    {
        var r = await api.GetAsync($"api/checkin/history/{siteType}/{siteId}?days={days}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<CheckInHistoryDto>>() ?? [] : [];
    }

    public async Task<string?> GenerateQrSlugAsync(string siteType, Guid siteId)
    {
        var r = await api.PostAsync($"api/checkin/generate-qr/{siteType}/{siteId}");
        if (r.IsSuccessStatusCode)
        {
            var result = await r.Content.ReadFromJsonAsync<QrSlugResult>();
            return result?.Slug;
        }
        return null;
    }

    public async Task<QrCodeInfoDto?> GetQrBrandingAsync(string siteType, Guid siteId)
    {
        var r = await api.GetAsync($"api/checkin/qr-branding/{siteType}/{siteId}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<QrCodeInfoDto>() : null;
    }

    public async Task<QrCodeInfoDto?> GetQrLandingInfoAsync(string slug)
    {
        // This endpoint is anonymous so we use the plain HttpClient
        var r = await api.GetAsync($"api/checkin/qr/{slug}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<QrCodeInfoDto>() : null;
    }

    private record QrSlugResult(string Slug, string Url);
}
