using System.Security.Claims;
using Solodoc.Application.Common;
using Solodoc.Application.Services;

namespace Solodoc.Api.Endpoints;

public static class FileEndpoints
{
    private static readonly HashSet<string> AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".heic"];

    public static WebApplication MapFileEndpoints(this WebApplication app)
    {
        app.MapPost("/api/files/upload", UploadFile)
            .RequireAuthorization()
            .DisableAntiforgery();

        app.MapGet("/api/files/{*key}", GetFileUrl)
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> UploadFile(
        HttpRequest request,
        IFileStorageService fileStorage,
        ITenantProvider tenantProvider,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Forventet multipart/form-data." });

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "Ingen fil mottatt." });

        if (file.Length > 25 * 1024 * 1024)
            return Results.BadRequest(new { error = "Filen er for stor. Maks 25MB." });

        // Validate file extension
        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        if (!AllowedExtensions.Contains(ext))
            return Results.BadRequest(new { error = $"Filtypen '{ext}' er ikke tillatt. Tillatte typer: {string.Join(", ", AllowedExtensions)}" });

        var tenantId = tenantProvider.TenantId.Value;
        var key = $"{tenantId}/checklists/{Guid.NewGuid()}{ext}";

        using var stream = file.OpenReadStream();
        await fileStorage.UploadFileAsync(stream, key, file.ContentType, ct);

        return Results.Ok(new { key });
    }

    private static async Task<IResult> GetFileUrl(
        string key,
        IFileStorageService fileStorage,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        // Prevent path traversal
        if (key.Contains("..") || key.Contains('\\'))
            return Results.BadRequest(new { error = "Ugyldig filsti." });

        // Verify the file belongs to the current tenant
        if (!key.StartsWith($"{tenantProvider.TenantId}/"))
            return Results.Unauthorized();

        try
        {
            var url = await fileStorage.GetPresignedUrlAsync(key, TimeSpan.FromMinutes(30), ct);
            return Results.Ok(new { url });
        }
        catch
        {
            return Results.NotFound();
        }
    }
}
