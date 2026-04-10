using System.Net.Http.Json;
using Solodoc.Shared.Dashboard;

namespace Solodoc.Client.Services;

public class DashboardService(ApiHttpClient api)
{
    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var response = await api.GetAsync("api/dashboard/summary");

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<DashboardSummaryDto>()
                   ?? Empty;

        return Empty;
    }

    private static readonly DashboardSummaryDto Empty = new(0, 0m, 0, [], []);
}
