using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Solodoc.Client.Services;

public class FileUploadService(ApiHttpClient api)
{
    public async Task<string?> UploadPhotoAsync(Stream stream, string fileName, string contentType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "application/octet-stream";

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);

            var response = await api.PostAsync("api/files/upload", content);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadResult>();
                return result?.Key;
            }
        }
        catch { }
        return null;
    }

    public async Task<string?> UploadFileAsync(MultipartFormDataContent content)
    {
        var response = await api.PostAsync("api/files/upload", content);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<UploadResult>();
            return result?.Key;
        }
        return null;
    }

    public async Task<string?> GetFileUrlAsync(string key)
    {
        var response = await api.GetAsync($"api/files/{key}");
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<UrlResult>();
            return result?.Url;
        }
        return null;
    }

    private record UploadResult(string Key);
    private record UrlResult(string Url);
}
