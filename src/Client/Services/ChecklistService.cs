using System.Net.Http.Json;
using Solodoc.Shared.Checklists;

namespace Solodoc.Client.Services;

public class ChecklistService(ApiHttpClient api, OfflineAwareApiClient offlineApi)
{
    private record IdResponse(Guid Id);

    // ─── Templates ───────────────────────────────────────

    public async Task<List<ChecklistTemplateListItemDto>> GetTemplatesAsync()
    {
        // Always fetch fresh — templates change frequently
        try
        {
            var response = await api.GetAsync("api/checklists/templates");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ChecklistTemplateListItemDto>>() ?? [];
        }
        catch { }
        // Fallback to cache if offline
        return await offlineApi.GetWithCacheAsync<List<ChecklistTemplateListItemDto>>(
            "api/checklists/templates", "checklistTemplates") ?? [];
    }

    public async Task<ChecklistTemplateDetailDto?> GetTemplateByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/checklists/templates/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ChecklistTemplateDetailDto>();
        return null;
    }

    public async Task<Guid?> CreateTemplateAsync(CreateChecklistTemplateRequest request)
    {
        var response = await api.PostAsJsonAsync("api/checklists/templates", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> UpdateTemplateAsync(Guid id, UpdateChecklistTemplateRequest request)
    {
        var response = await api.PutAsJsonAsync($"api/checklists/templates/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> AddTemplateItemAsync(Guid templateId, AddTemplateItemRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/checklists/templates/{templateId}/items", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> UpdateTemplateItemAsync(Guid templateId, Guid itemId, UpdateTemplateItemRequest request)
    {
        var response = await api.PutAsJsonAsync($"api/checklists/templates/{templateId}/items/{itemId}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteTemplateItemAsync(Guid templateId, Guid itemId)
    {
        var response = await api.DeleteAsync($"api/checklists/templates/{templateId}/items/{itemId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PublishTemplateAsync(Guid id)
    {
        var response = await api.PostAsync($"api/checklists/templates/{id}/publish");
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> DuplicateTemplateAsync(Guid id)
    {
        var response = await api.PostAsync($"api/checklists/templates/{id}/duplicate");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> DeleteTemplateAsync(Guid id)
    {
        var response = await api.DeleteAsync($"api/checklists/templates/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<(Guid? Id, string? Error)> ImportTemplateFromFileAsync(MultipartFormDataContent content)
    {
        var response = await api.PostAsync("api/checklists/templates/import", content);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return (result?.Id, null);
        }

        // Try to read error message from response
        try
        {
            var errorJson = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return (null, errorJson?.Error ?? $"Feil ({(int)response.StatusCode})");
        }
        catch
        {
            return (null, response.StatusCode switch
            {
                System.Net.HttpStatusCode.RequestEntityTooLarge => "Filen er for stor. Maks 25 MB.",
                System.Net.HttpStatusCode.Unauthorized => "Du er ikke logget inn. Logg inn og prov igjen.",
                System.Net.HttpStatusCode.BadRequest => "Kunne ikke lese filen. Prov et annet format (PDF, Excel, Word).",
                _ => $"Uventet feil ({(int)response.StatusCode})"
            });
        }
    }

    private record ErrorResponse(string? Error);

    // ─── Instances ───────────────────────────────────────

    public async Task<List<ChecklistInstanceListItemDto>> GetInstancesAsync(Guid? projectId = null, Guid? locationId = null)
    {
        var url = "api/checklists/instances";
        var sep = '?';
        if (projectId.HasValue) { url += $"{sep}projectId={projectId.Value}"; sep = '&'; }
        if (locationId.HasValue) { url += $"{sep}locationId={locationId.Value}"; }
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ChecklistInstanceListItemDto>>() ?? [];
        return [];
    }

    public async Task<ChecklistInstanceDetailDto?> GetInstanceDetailAsync(Guid id)
    {
        return await offlineApi.GetWithCacheAsync<ChecklistInstanceDetailDto>(
            $"api/checklists/instances/{id}", "checklistInstance");
    }

    public async Task<Guid?> CreateInstanceAsync(CreateChecklistInstanceRequest request)
    {
        var response = await api.PostAsJsonAsync("api/checklists/instances", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> DeleteInstanceAsync(Guid id)
    {
        var response = await api.DeleteAsync($"api/checklists/instances/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SubmitItemAsync(Guid instanceId, Guid itemId, SubmitChecklistItemRequest request)
    {
        return await offlineApi.PutWithQueueAsync(
            $"api/checklists/instances/{instanceId}/items/{itemId}",
            "checklistItem", request);
    }

    public async Task<bool> SubmitInstanceAsync(Guid id)
    {
        return await offlineApi.PatchWithQueueAsync(
            $"api/checklists/instances/{id}/submit",
            "checklistInstance");
    }

    public async Task<bool> ApproveInstanceAsync(Guid id)
    {
        var response = await api.PatchAsync($"api/checklists/instances/{id}/approve");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ReopenInstanceAsync(Guid id, ReopenInstanceRequest request)
    {
        var response = await api.PatchAsJsonAsync($"api/checklists/instances/{id}/reopen", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> DuplicateInstanceAsync(Guid id, DuplicateInstanceRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/checklists/instances/{id}/duplicate", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<BatchDuplicateResponse?> BatchDuplicateAsync(BatchDuplicateRequest request)
    {
        var response = await api.PostAsJsonAsync("api/checklists/instances/batch-duplicate", request);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<BatchDuplicateResponse>();
        return null;
    }

    // ─── Participants ───

    public async Task<List<ChecklistParticipantDto>> GetParticipantsAsync(Guid instanceId)
    {
        var r = await api.GetAsync($"api/checklists/instances/{instanceId}/participants");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<ChecklistParticipantDto>>() ?? [] : [];
    }

    public async Task<bool> AddParticipantAsync(Guid instanceId, Guid personId)
    {
        var r = await api.PostAsJsonAsync($"api/checklists/instances/{instanceId}/participants", new AddChecklistParticipantRequest(personId));
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> AddExternalParticipantAsync(Guid instanceId, string name, string? phone, string? company)
    {
        var r = await api.PostAsJsonAsync($"api/checklists/instances/{instanceId}/participants/external", new AddExternalChecklistParticipantRequest(name, phone, company));
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveParticipantAsync(Guid instanceId, Guid participantId)
    {
        var r = await api.DeleteAsync($"api/checklists/instances/{instanceId}/participants/{participantId}");
        return r.IsSuccessStatusCode;
    }
}
