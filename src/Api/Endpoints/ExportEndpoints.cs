using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Export;

namespace Solodoc.Api.Endpoints;

public static class ExportEndpoints
{
    public static WebApplication MapExportEndpoints(this WebApplication app)
    {
        app.MapPost("/api/export/project/{id:guid}", CreateProjectExport).RequireAuthorization();
        app.MapPost("/api/export/employee/{id:guid}", CreateEmployeeExport).RequireAuthorization();
        app.MapPost("/api/export/custom", CreateCustomExport).RequireAuthorization();
        app.MapGet("/api/export/{id:guid}/status", GetExportStatus).RequireAuthorization();
        app.MapGet("/api/export/{id:guid}/download", DownloadExport).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> CreateProjectExport(
        Guid id,
        CreateProjectExportRequest request,
        ClaimsPrincipal user,
        IExportService exportService,
        IServiceScopeFactory scopeFactory,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId) || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var jobId = await exportService.CreateProjectExportAsync(
            id, request.OutputMode, request.PhotoOption, personId, tenantProvider.TenantId.Value, ct);

        // Process in background
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IExportService>();
            await service.ProcessExportAsync(jobId);
        });

        return Results.Accepted($"/api/export/{jobId}/status", new { id = jobId });
    }

    private static async Task<IResult> CreateEmployeeExport(
        Guid id,
        CreateEmployeeExportRequest request,
        ClaimsPrincipal user,
        IExportService exportService,
        IServiceScopeFactory scopeFactory,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId) || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var jobId = await exportService.CreateEmployeeExportAsync(
            id, request.OutputMode, personId, tenantProvider.TenantId.Value, ct);

        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IExportService>();
            await service.ProcessExportAsync(jobId);
        });

        return Results.Accepted($"/api/export/{jobId}/status", new { id = jobId });
    }

    private static async Task<IResult> CreateCustomExport(
        CreateCustomExportRequest request,
        ClaimsPrincipal user,
        IExportService exportService,
        IServiceScopeFactory scopeFactory,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId) || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (request.Items is null || request.Items.Count == 0)
            return Results.BadRequest(new { error = "Ingen elementer valgt for eksport." });

        var selectionJson = JsonSerializer.Serialize(request.Items, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var jobId = await exportService.CreateCustomExportAsync(
            request.OutputMode, request.PhotoOption, selectionJson, personId, tenantProvider.TenantId.Value, ct);

        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IExportService>();
            await service.ProcessExportAsync(jobId);
        });

        return Results.Accepted($"/api/export/{jobId}/status", new { id = jobId });
    }

    private static async Task<IResult> GetExportStatus(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var job = await db.ExportJobs
            .Where(j => j.Id == id && j.TenantId == tenantProvider.TenantId.Value)
            .Select(j => new ExportJobDto(
                j.Id,
                j.Type,
                j.Status,
                j.OutputMode,
                j.ProgressPercent,
                j.ResultFileName,
                j.ResultFileSizeBytes,
                j.CreatedAt,
                j.CompletedAt,
                j.ErrorMessage))
            .FirstOrDefaultAsync(ct);

        return job is not null ? Results.Ok(job) : Results.NotFound();
    }

    private static async Task<IResult> DownloadExport(
        Guid id,
        SolodocDbContext db,
        IFileStorageService fileStorage,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var job = await db.ExportJobs.FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantProvider.TenantId.Value, ct);
        if (job is null)
            return Results.NotFound();

        if (job.Status != "Completed" || string.IsNullOrEmpty(job.ResultFileKey))
            return Results.BadRequest(new { error = "Eksport er ikke klar for nedlasting." });

        var url = await fileStorage.GetPresignedUrlAsync(job.ResultFileKey, TimeSpan.FromMinutes(15), ct);
        return Results.Redirect(url);
    }
}
