using System.Net.Http.Json;
using Solodoc.Shared.Locations;

namespace Solodoc.Client.Services;

public class LocationService(ApiHttpClient api, OfflineAwareApiClient offlineApi)
{
    public async Task<List<LocationListItemDto>> GetLocationsAsync()
    {
        return await offlineApi.GetWithCacheAsync<List<LocationListItemDto>>("api/locations", "locations") ?? [];
    }

    public async Task<LocationDetailDto?> GetLocationAsync(Guid id)
    {
        var r = await api.GetAsync($"api/locations/{id}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<LocationDetailDto>() : null;
    }

    public async Task<Guid?> CreateLocationAsync(CreateLocationRequest req)
    {
        var r = await api.PostAsJsonAsync("api/locations", req);
        if (r.IsSuccessStatusCode) { var res = await r.Content.ReadFromJsonAsync<IdRes>(); return res?.Id; }
        return null;
    }

    public async Task<bool> UpdateLocationAsync(Guid id, CreateLocationRequest req)
    {
        return (await api.PutAsJsonAsync($"api/locations/{id}", req)).IsSuccessStatusCode;
    }

    public async Task<bool> DeleteLocationAsync(Guid id)
    {
        return (await api.DeleteAsync($"api/locations/{id}")).IsSuccessStatusCode;
    }

    public async Task<List<LocationTemplateDto>> GetLocationTemplatesAsync(Guid locationId)
    {
        var r = await api.GetAsync($"api/locations/{locationId}/templates");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<LocationTemplateDto>>() ?? [] : [];
    }

    public async Task<bool> AssignTemplateAsync(Guid locationId, Guid templateId)
    {
        return (await api.PostAsJsonAsync($"api/locations/{locationId}/templates",
            new AssignTemplateToLocationRequest(templateId, locationId))).IsSuccessStatusCode;
    }

    public async Task<bool> RemoveTemplateAsync(Guid locationId, Guid templateId)
    {
        return (await api.DeleteAsync($"api/locations/{locationId}/templates/{templateId}")).IsSuccessStatusCode;
    }

    private record IdRes(Guid Id);
}

// DTO for template list within a location
public record LocationTemplateDto(Guid Id, string Name, string? Description, string? DocumentType, string? DocumentNumber);
