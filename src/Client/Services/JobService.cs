using System.Net.Http.Json;
using Solodoc.Shared.Common;
using Solodoc.Shared.Projects;

namespace Solodoc.Client.Services;

public class JobService(ApiHttpClient api)
{
    public async Task<PagedResult<JobListItemDto>> GetJobsAsync(
        int page, int pageSize, string? search, string? sortBy, bool sortDesc)
    {
        var url = $"api/jobs?page={page}&pageSize={pageSize}&sortDesc={sortDesc}";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(sortBy))
            url += $"&sortBy={Uri.EscapeDataString(sortBy)}";

        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<PagedResult<JobListItemDto>>()
                   ?? new([], 0, page, pageSize);
        return new([], 0, page, pageSize);
    }

    public async Task<JobDetailDto?> GetJobByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/jobs/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<JobDetailDto>();
        return null;
    }

    public async Task<Guid?> CreateJobAsync(CreateJobRequest request)
    {
        var response = await api.PostAsJsonAsync("api/jobs", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> UpdateJobAsync(Guid id, UpdateJobRequest request)
    {
        var response = await api.PutAsJsonAsync($"api/jobs/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status)
    {
        var response = await api.PatchAsJsonAsync($"api/jobs/{id}/status", new { Status = status });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AddPartsItemAsync(Guid jobId, AddPartsItemRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/jobs/{jobId}/parts", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PromoteToProjectAsync(Guid jobId)
    {
        var response = await api.PostAsJsonAsync($"api/jobs/{jobId}/promote", new { });
        return response.IsSuccessStatusCode;
    }

    private record IdResponse(Guid Id);
}
