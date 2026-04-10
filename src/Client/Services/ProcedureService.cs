using System.Net.Http.Json;
using Solodoc.Shared.Checklists;
using Solodoc.Shared.Procedures;

namespace Solodoc.Client.Services;

public class ProcedureService(ApiHttpClient api)
{
    public async Task<List<ProcedureListItemDto>> GetProceduresAsync()
    {
        var response = await api.GetAsync("api/procedures");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ProcedureListItemDto>>() ?? [];
        return [];
    }

    public async Task<ProcedureDetailDto?> GetProcedureByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/procedures/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProcedureDetailDto>();
        return null;
    }

    public async Task<Guid?> CreateProcedureAsync(CreateProcedureRequest request)
    {
        var response = await api.PostAsJsonAsync("api/procedures", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> MarkReadAsync(Guid procedureId)
    {
        var response = await api.PostAsync($"api/procedures/{procedureId}/mark-read");
        return response.IsSuccessStatusCode;
    }

    public async Task<ProcedureReadStatusDto?> GetReadStatusAsync(Guid procedureId)
    {
        var response = await api.GetAsync($"api/procedures/{procedureId}/read-status");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProcedureReadStatusDto>();
        return null;
    }

    private record IdResponse(Guid Id);
}
