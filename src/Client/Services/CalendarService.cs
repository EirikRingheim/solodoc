using System.Net.Http.Json;
using Solodoc.Shared.Calendar;

namespace Solodoc.Client.Services;

public class CalendarService(ApiHttpClient api)
{
    public async Task<List<CalendarEventDto>> GetEventsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var url = "api/calendar/events";
        var sep = '?';
        if (from.HasValue) { url += $"{sep}from={from.Value.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}"; sep = '&'; }
        if (to.HasValue) url += $"{sep}to={to.Value.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";

        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<CalendarEventDto>>() ?? [];
        return [];
    }

    public async Task<Guid?> CreateEventAsync(CreateCalendarEventRequest request)
    {
        var response = await api.PostAsJsonAsync("api/calendar/events", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<List<CrossTenantCalendarEventDto>> GetCrossTenantEventsAsync(DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        var url = "api/calendar/cross-tenant";
        var sep = '?';
        if (from.HasValue) { url += $"{sep}from={from.Value.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}"; sep = '&'; }
        if (to.HasValue) url += $"{sep}to={to.Value.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}";

        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<CrossTenantCalendarEventDto>>() ?? [];
        return [];
    }

    private record IdResponse(Guid Id);
}
