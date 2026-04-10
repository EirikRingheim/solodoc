using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Solodoc.Infrastructure.Services;

namespace Solodoc.IntegrationTests;

public class FileStorageServiceTests
{
    private readonly FileStorageService? _sut;
    private readonly bool _minioAvailable;

    public FileStorageServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Endpoint"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "minioadmin",
                ["Storage:SecretKey"] = "minioadmin",
                ["Storage:BucketName"] = "solodoc-dev"
            })
            .Build();

        try
        {
            _sut = new FileStorageService(config);
            _minioAvailable = true;
        }
        catch
        {
            _minioAvailable = false;
        }
    }

    [Fact]
    public async Task UploadAndDownload_RoundTrip_ReturnsOriginalContent()
    {
        if (!_minioAvailable || _sut is null)
        {
            // Skip if MinIO is not available
            return;
        }

        var key = $"test/{Guid.NewGuid()}/test.txt";
        var content = "Hello, Solodoc!"u8.ToArray();

        try
        {
            using var uploadStream = new MemoryStream(content);
            var resultKey = await _sut.UploadFileAsync(uploadStream, key, "text/plain");
            resultKey.Should().Be(key);

            using var downloadStream = await _sut.DownloadFileAsync(key);
            using var ms = new MemoryStream();
            await downloadStream.CopyToAsync(ms);
            ms.ToArray().Should().BeEquivalentTo(content);
        }
        catch (Exception ex) when (ex.Message.Contains("connect") || ex.Message.Contains("refused") || ex.Message.Contains("timeout"))
        {
            // MinIO not running, skip
        }
        finally
        {
            try { await _sut.DeleteFileAsync(key); } catch { /* cleanup best-effort */ }
        }
    }

    [Fact]
    public async Task GetPresignedUrl_ReturnsValidUrl()
    {
        if (!_minioAvailable || _sut is null) return;

        var key = "test/presigned-test.txt";

        try
        {
            var url = await _sut.GetPresignedUrlAsync(key, TimeSpan.FromMinutes(5));
            url.Should().NotBeNullOrWhiteSpace();
            url.Should().Contain(key);
        }
        catch (Exception ex) when (ex.Message.Contains("connect") || ex.Message.Contains("refused") || ex.Message.Contains("timeout"))
        {
            // MinIO not running, skip
        }
    }

    [Fact]
    public async Task DeleteFile_DoesNotThrow_WhenKeyExists()
    {
        if (!_minioAvailable || _sut is null) return;

        var key = $"test/{Guid.NewGuid()}/delete-test.txt";

        try
        {
            using var uploadStream = new MemoryStream("test"u8.ToArray());
            await _sut.UploadFileAsync(uploadStream, key, "text/plain");

            var act = () => _sut.DeleteFileAsync(key);
            await act.Should().NotThrowAsync();
        }
        catch (Exception ex) when (ex.Message.Contains("connect") || ex.Message.Contains("refused") || ex.Message.Contains("timeout"))
        {
            // MinIO not running, skip
        }
    }
}
