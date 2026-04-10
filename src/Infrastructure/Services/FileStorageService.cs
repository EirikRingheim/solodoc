using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Solodoc.Application.Services;

namespace Solodoc.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public FileStorageService(IConfiguration configuration)
    {
        var endpoint = configuration["Storage:Endpoint"] ?? "http://localhost:9000";
        var accessKey = configuration["Storage:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["Storage:SecretKey"] ?? "minioadmin";
        _bucketName = configuration["Storage:BucketName"] ?? "solodoc-dev";

        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true
        };
        _s3 = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadFileAsync(Stream stream, string key, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        };
        await _s3.PutObjectAsync(request, ct);
        return key;
    }

    public async Task<Stream> DownloadFileAsync(string key, CancellationToken ct = default)
    {
        var response = await _s3.GetObjectAsync(_bucketName, key, ct);
        return response.ResponseStream;
    }

    public Task<string> GetPresignedUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };
        // GetPreSignedURL is synchronous in AWSSDK.S3 v4
        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(url);
    }

    public async Task DeleteFileAsync(string key, CancellationToken ct = default)
    {
        await _s3.DeleteObjectAsync(_bucketName, key, ct);
    }
}
