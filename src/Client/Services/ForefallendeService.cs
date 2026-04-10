using System.Net.Http.Json;
using Solodoc.Shared.Forefallende;

namespace Solodoc.Client.Services;

public class ForefallendeService(ApiHttpClient api)
{
    public async Task<List<ForefallendItemDto>> GetItemsAsync()
    {
        var response = await api.GetAsync("api/forefallende");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ForefallendItemDto>>() ?? [];
        return [];
    }
}
