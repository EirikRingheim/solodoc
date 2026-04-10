using System.Net.Http.Json;
using Solodoc.Shared.Search;

namespace Solodoc.Client.Services;

public class SearchService(ApiHttpClient api)
{
    public async Task<SearchResponse> SearchAsync(string query)
    {
        var url = $"api/search?q={Uri.EscapeDataString(query)}";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SearchResponse>()
                   ?? new SearchResponse([], 0);
        return new SearchResponse([], 0);
    }
}
