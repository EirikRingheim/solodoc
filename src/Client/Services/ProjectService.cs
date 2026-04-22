using System.Net.Http.Json;
using Solodoc.Shared.Common;
using Solodoc.Shared.Projects;
using Microsoft.AspNetCore.Components;

namespace Solodoc.Client.Services;

public class ProjectService(ApiHttpClient api, OfflineAwareApiClient offlineApi)
{
    private record IdResponse(Guid Id);

    public async Task<PagedResult<ProjectListItemDto>> GetProjectsAsync(
        int page, int pageSize, string? search, string? sortBy, bool sortDesc,
        bool topLevelOnly = false)
    {
        var url = $"api/projects?page={page}&pageSize={pageSize}&sortDesc={sortDesc}";
        if (topLevelOnly) url += "&topLevelOnly=true";

        if (!string.IsNullOrWhiteSpace(search))
            url += $"&search={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrWhiteSpace(sortBy))
            url += $"&sortBy={Uri.EscapeDataString(sortBy)}";

        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<PagedResult<ProjectListItemDto>>()
                   ?? new([], 0, page, pageSize);

        return new([], 0, page, pageSize);
    }

    public async Task<List<ProjectListItemDto>> GetActiveProjectsAsync()
    {
        // Try with offline cache support
        var cached = await offlineApi.GetWithCacheAsync<PagedResult<ProjectListItemDto>>(
            "api/projects?page=1&pageSize=100&sortDesc=false&sortBy=name", "projects");
        if (cached is not null) return cached.Items.ToList();

        var result = await GetProjectsAsync(1, 100, null, "name", false);
        return result.Items.ToList();
    }

    public async Task<ProjectDetailDto?> GetProjectByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/projects/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProjectDetailDto>();
        return null;
    }

    public async Task<Guid?> CreateProjectAsync(CreateProjectRequest request)
    {
        var response = await api.PostAsJsonAsync("api/projects", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> UpdateProjectAsync(Guid id, UpdateProjectRequest request)
    {
        var response = await api.PutAsJsonAsync($"api/projects/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ChangeStatusAsync(Guid id, string status)
    {
        var response = await api.PatchAsJsonAsync($"api/projects/{id}/status", new ChangeStatusRequest(status));
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteProjectAsync(Guid id)
    {
        var response = await api.DeleteAsync($"api/projects/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<SubProjectSummaryDto>> GetSubProjectsAsync(Guid parentId)
    {
        var response = await api.GetAsync($"api/projects/{parentId}/subprojects");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<List<SubProjectSummaryDto>>() ?? []
            : [];
    }

    public async Task<List<ProjectListItemDto>> GetTopLevelProjectsAsync()
    {
        var result = await GetProjectsAsync(1, 100, null, "name", false);
        return result.Items.Where(p => p.ParentProjectId is null).ToList();
    }

    public async Task<bool> UpdateGeofenceAsync(Guid projectId, UpdateProjectGeofenceRequest request)
    {
        var r = await api.PostAsJsonAsync($"api/projects/{projectId}/site-boundary", request);
        return r.IsSuccessStatusCode;
    }
}
