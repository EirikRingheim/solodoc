using System.Net.Http.Json;
using Solodoc.Shared.Hms;

namespace Solodoc.Client.Services;

public class HmsService(ApiHttpClient api)
{
    public async Task<List<SjaFormListItemDto>> GetSjaFormsAsync()
    {
        var response = await api.GetAsync("api/hms/sja");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<SjaFormListItemDto>>() ?? [];
        return [];
    }

    public async Task<SjaFormDetailDto?> GetSjaFormByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/hms/sja/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SjaFormDetailDto>();
        return null;
    }

    public async Task<Guid?> CreateSjaFormAsync(CreateSjaFormRequest request)
    {
        var response = await api.PostAsJsonAsync("api/hms/sja", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> AddHazardAsync(Guid sjaId, AddSjaHazardRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/hms/sja/{sjaId}/hazards", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<HmsMeetingListItemDto>> GetMeetingsAsync()
    {
        var response = await api.GetAsync("api/hms/meetings");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<HmsMeetingListItemDto>>() ?? [];
        return [];
    }

    public async Task<Guid?> CreateMeetingAsync(CreateHmsMeetingRequest request)
    {
        var response = await api.PostAsJsonAsync("api/hms/meetings", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<HmsMeetingDetailDto?> GetMeetingDetailAsync(Guid id)
    {
        var response = await api.GetAsync($"api/hms/meetings/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<HmsMeetingDetailDto>();
        return null;
    }

    public async Task<bool> AddActionItemAsync(Guid meetingId, CreateActionItemRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/hms/meetings/{meetingId}/action-items", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateMinutesAsync(Guid meetingId, string minutes)
    {
        var response = await api.PutAsJsonAsync($"api/hms/meetings/{meetingId}/minutes",
            new UpdateMinutesRequest(minutes));
        return response.IsSuccessStatusCode;
    }

    public async Task<List<SafetyRoundScheduleDto>> GetSafetyRoundSchedulesAsync()
    {
        var response = await api.GetAsync("api/hms/safety-round-schedules");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<SafetyRoundScheduleDto>>() ?? [];
        return [];
    }

    private record IdResponse(Guid Id);
}
