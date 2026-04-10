namespace Solodoc.Application.Services;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream stream, string key, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadFileAsync(string key, CancellationToken ct = default);
    Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);
    Task DeleteFileAsync(string key, CancellationToken ct = default);
}
