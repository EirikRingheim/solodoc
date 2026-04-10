using System.Net.Http.Json;
using Solodoc.Shared.Chemicals;

namespace Solodoc.Client.Services;

public class ChemicalService(ApiHttpClient api)
{
    public async Task<List<ChemicalListItemDto>> GetChemicalsAsync(string? search = null)
    {
        var url = "api/chemicals";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChemicalListItemDto>>() ?? [];
        return [];
    }

    public async Task<ChemicalDetailDto?> GetChemicalByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/chemicals/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChemicalDetailDto>();
        return null;
    }

    public async Task<Guid?> CreateChemicalAsync(CreateChemicalRequest request)
    {
        var response = await api.PostAsJsonAsync("api/chemicals", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    private record IdResponse(Guid Id);
}
