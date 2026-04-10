using System.Net.Http.Json;
using Solodoc.Shared.Common;
using Solodoc.Shared.Deviations;

namespace Solodoc.Client.Services;

public class DeviationService(ApiHttpClient api)
{
    public async Task<PagedResult<DeviationListItemDto>> GetDeviationsAsync(
        int page, int pageSize, string? search, string? sortBy, bool sortDesc)
    {
        var url = $"api/deviations?page={page}&pageSize={pageSize}&sortDesc={sortDesc}";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(sortBy))
            url += $"&sortBy={Uri.EscapeDataString(sortBy)}";

        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<PagedResult<DeviationListItemDto>>()
                   ?? new([], 0, page, pageSize);

        return new([], 0, page, pageSize);
    }

    public async Task<int> GetOpenCountAsync()
    {
        var response = await api.GetAsync("api/deviations/open-count");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<OpenCountResponse>();
            return result?.Count ?? 0;
        }
        return 0;
    }

    public async Task<Guid?> CreateDeviationAsync(CreateDeviationRequest request)
    {
        var response = await api.PostAsJsonAsync("api/deviations", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<CreateResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<DeviationDetailDto?> GetDeviationByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/deviations/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<DeviationDetailDto>();
        return null;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string newStatus)
    {
        var response = await api.PutAsJsonAsync($"api/deviations/{id}/status",
            new UpdateDeviationStatusRequest(newStatus));
        return response.IsSuccessStatusCode;
    }

    public async Task<PagedResult<DeviationListItemDto>> GetDeviationsForProjectAsync(
        Guid projectId, int page = 1, int pageSize = 50)
    {
        var url = $"api/deviations?page={page}&pageSize={pageSize}&projectId={projectId}";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<PagedResult<DeviationListItemDto>>()
                   ?? new([], 0, page, pageSize);
        return new([], 0, page, pageSize);
    }

    public async Task<PagedResult<DeviationListItemDto>> GetDeviationsForLocationAsync(
        Guid locationId, int page = 1, int pageSize = 50)
    {
        var url = $"api/deviations?page={page}&pageSize={pageSize}&locationId={locationId}";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<PagedResult<DeviationListItemDto>>()
                   ?? new([], 0, page, pageSize);
        return new([], 0, page, pageSize);
    }

    public async Task<List<DeviationCategoryDto>> GetCategoriesAsync()
    {
        var response = await api.GetAsync("api/deviations/categories");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<DeviationCategoryDto>>() ?? [];
        return [];
    }

    public async Task<bool> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var response = await api.PostAsJsonAsync("api/deviations/categories", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        var response = await api.DeleteAsync($"api/deviations/categories/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<DeviationCommentDto>> GetCommentsAsync(Guid deviationId)
    {
        var response = await api.GetAsync($"api/deviations/{deviationId}/comments");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<DeviationCommentDto>>() ?? [];
        return [];
    }

    public async Task<bool> AddCommentAsync(Guid deviationId, string text)
    {
        var response = await api.PostAsJsonAsync($"api/deviations/{deviationId}/comments", new AddCommentRequest(text));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AssignAsync(Guid id, Guid assignedToId, DateTimeOffset? deadline)
    {
        var response = await api.PatchAsJsonAsync($"api/deviations/{id}/assign",
            new AssignDeviationRequest(assignedToId, deadline));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CloseAsync(Guid id, string? correctiveAction)
    {
        var response = await api.PatchAsJsonAsync($"api/deviations/{id}/close",
            new CloseDeviationRequest(correctiveAction, DateTimeOffset.UtcNow));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReopenAsync(Guid id)
    {
        var response = await api.PatchAsync($"api/deviations/{id}/reopen");
        return response.IsSuccessStatusCode;
    }

    private record OpenCountResponse(int Count);
    private record CreateResponse(Guid Id);
}
