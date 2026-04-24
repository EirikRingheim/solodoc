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

    public async Task<List<EquipmentListItemDto>> GetEquipmentByProjectAsync(Guid projectId)
    {
        var response = await api.GetAsync($"api/equipment/by-project/{projectId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<EquipmentListItemDto>>() ?? [];
        return [];
    }

    public async Task<List<EquipmentListItemDto>> GetEquipmentByJobAsync(Guid jobId)
    {
        var response = await api.GetAsync($"api/equipment/by-job/{jobId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<EquipmentListItemDto>>() ?? [];
        return [];
    }

    public async Task<List<EquipmentListItemDto>> GetEquipmentByLocationAsync(Guid locationId)
    {
        var response = await api.GetAsync($"api/equipment/by-location/{locationId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<EquipmentListItemDto>>() ?? [];
        return [];
    }

    public async Task<bool> UpdateLocationAsync(Guid id, UpdateEquipmentLocationRequest request)
    {
        var r = await api.PostAsJsonAsync($"api/equipment/{id}/location", request);
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> AssignToProjectAsync(Guid equipmentId, AssignEquipmentToProjectRequest request)
    {
        var r = await api.PostAsJsonAsync($"api/equipment/{equipmentId}/assign", request);
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveAssignmentAsync(Guid equipmentId, Guid assignmentId)
    {
        var r = await api.DeleteAsync($"api/equipment/{equipmentId}/assign/{assignmentId}");
        return r.IsSuccessStatusCode;
    }

    public async Task<List<EquipmentTypeCategoryDto>> GetTypeCategoriesAsync()
    {
        var response = await api.GetAsync("api/equipment/type-categories");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<EquipmentTypeCategoryDto>>() ?? [];
        return [];
    }

    public async Task<bool> CreateTypeCategoryAsync(string name)
    {
        var response = await api.PostAsJsonAsync("api/equipment/type-categories",
            new CreateEquipmentTypeCategoryRequest(name));
        return response.IsSuccessStatusCode;
    }

    private record IdResponse(Guid Id);
}
