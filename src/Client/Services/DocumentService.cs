using System.Net.Http.Json;
using Solodoc.Shared.Documents;

namespace Solodoc.Client.Services;

public class DocumentService(ApiHttpClient api)
{
    public async Task<List<DocumentFolderDto>> GetFoldersAsync(Guid? projectId = null, Guid? parentId = null)
    {
        var url = "api/documents/folders?";
        if (projectId.HasValue) url += $"projectId={projectId}&";
        if (parentId.HasValue) url += $"parentId={parentId}&";

        var r = await api.GetAsync(url.TrimEnd('&', '?'));
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<DocumentFolderDto>>() ?? [] : [];
    }

    public async Task<Guid?> CreateFolderAsync(CreateFolderRequest request)
    {
        var r = await api.PostAsJsonAsync("api/documents/folders", request);
        if (r.IsSuccessStatusCode)
        {
            var result = await r.Content.ReadFromJsonAsync<IdResult>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> UpdateFolderAsync(Guid id, CreateFolderRequest request)
        => (await api.PutAsJsonAsync($"api/documents/folders/{id}", request)).IsSuccessStatusCode;

    public async Task<bool> DeleteFolderAsync(Guid id)
        => (await api.DeleteAsync($"api/documents/folders/{id}")).IsSuccessStatusCode;

    public async Task<List<DocumentDto>> GetDocumentsAsync(Guid? folderId = null, string? search = null, string? sortBy = null)
    {
        var url = "api/documents?";
        if (folderId.HasValue) url += $"folderId={folderId}&";
        if (!string.IsNullOrEmpty(search)) url += $"search={Uri.EscapeDataString(search)}&";
        if (!string.IsNullOrEmpty(sortBy)) url += $"sortBy={sortBy}&";

        var r = await api.GetAsync(url.TrimEnd('&', '?'));
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<DocumentDto>>() ?? [] : [];
    }

    public async Task<Guid?> UploadDocumentAsync(MultipartFormDataContent content)
    {
        var r = await api.PostAsync("api/documents/upload", content);
        if (r.IsSuccessStatusCode)
        {
            var result = await r.Content.ReadFromJsonAsync<IdResult>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> DeleteDocumentAsync(Guid id)
        => (await api.DeleteAsync($"api/documents/{id}")).IsSuccessStatusCode;

    private record IdResult(Guid Id);
}
