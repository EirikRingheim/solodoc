using System.Net.Http.Json;
using Solodoc.Shared.Equipment;

namespace Solodoc.Client.Services;

public class EquipmentService(ApiHttpClient api)
{
    public async Task<List<EquipmentListItemDto>> GetEquipmentAsync(string? search = null)
    {
        var url = "api/equipment";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<EquipmentListItemDto>>() ?? [];
        return [];
    }

    public async Task<EquipmentDetailDto?> GetEquipmentByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/equipment/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<EquipmentDetailDto>();
        return null;
    }

    public async Task<Guid?> CreateEquipmentAsync(CreateEquipmentRequest request)
    {
        var response = await api.PostAsJsonAsync("api/equipment", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> UpdateEquipmentAsync(Guid id, CreateEquipmentRequest request)
    {
        var response = await api.PutAsJsonAsync($"api/equipment/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddMaintenanceAsync(Guid equipmentId, AddMaintenanceRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/equipment/{equipmentId}/maintenance", request);
        return response.IsSuccessStatusCode;
    }

    private record IdResponse(Guid Id);
}
