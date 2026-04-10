using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Export;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Export;

namespace Solodoc.Infrastructure.Services;

public class ExportService(
    SolodocDbContext db,
    IPdfReportService pdfService,
    IFileStorageService fileStorage,
    ILogger<ExportService> logger) : IExportService
{
    public async Task<Guid> CreateProjectExportAsync(Guid projectId, string outputMode, string? photoOption, Guid requestedById, Guid tenantId, CancellationToken ct = default)
    {
        var job = new ExportJob
        {
            TenantId = tenantId,
            Type = "project",
            TargetEntityId = projectId,
            OutputMode = outputMode,
            PhotoOption = photoOption,
            Status = "Pending",
            RequestedById = requestedById,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        db.ExportJobs.Add(job);
        await db.SaveChangesAsync(ct);
        return job.Id;
    }

    public async Task<Guid> CreateEmployeeExportAsync(Guid personId, string outputMode, Guid requestedById, Guid tenantId, CancellationToken ct = default)
    {
        var job = new ExportJob
        {
            TenantId = tenantId,
            Type = "employee",
            TargetEntityId = personId,
            OutputMode = outputMode,
            Status = "Pending",
            RequestedById = requestedById,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        db.ExportJobs.Add(job);
        await db.SaveChangesAsync(ct);
        return job.Id;
    }

    public async Task<Guid> CreateCustomExportAsync(string outputMode, string? photoOption, string selectionJson, Guid requestedById, Guid tenantId, CancellationToken ct = default)
    {
        var job = new ExportJob
        {
            TenantId = tenantId,
            Type = "custom",
            OutputMode = outputMode,
            PhotoOption = photoOption,
            SelectionJson = selectionJson,
            Status = "Pending",
            RequestedById = requestedById,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        db.ExportJobs.Add(job);
        await db.SaveChangesAsync(ct);
        return job.Id;
    }

    public async Task ProcessExportAsync(Guid exportJobId, CancellationToken ct = default)
    {
        var job = await db.ExportJobs.FirstOrDefaultAsync(j => j.Id == exportJobId, ct);
        if (job is null) return;

        try
        {
            job.Status = "Processing";
            job.ProgressPercent = 0;
            await db.SaveChangesAsync(ct);

            var files = new List<(string name, byte[] data)>();

            switch (job.Type)
            {
                case "project":
                    await GenerateProjectFiles(job, files, ct);
                    break;
                case "employee":
                    await GenerateEmployeeFiles(job, files, ct);
                    break;
                case "custom":
                    await GenerateCustomFiles(job, files, ct);
                    break;
            }

            // Upload result based on output mode
            byte[] resultBytes;
            string fileName;

            if (job.OutputMode == "CombinedPdf" && files.Count == 1)
            {
                resultBytes = files[0].data;
                fileName = files[0].name;
            }
            else if (job.OutputMode == "StructuredZip" || files.Count > 1)
            {
                using var ms = new MemoryStream();
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (var (name, data) in files)
                    {
                        var entry = archive.CreateEntry(name);
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(data, ct);
                    }
                }
                resultBytes = ms.ToArray();
                fileName = $"export-{job.Id:N}.zip";
            }
            else if (files.Count == 0)
            {
                throw new InvalidOperationException("No files generated for export.");
            }
            else
            {
                resultBytes = files[0].data;
                fileName = files[0].name;
            }

            // Upload to storage
            var key = $"{job.TenantId}/exports/{job.Id}/{fileName}";
            using var uploadStream = new MemoryStream(resultBytes);
            await fileStorage.UploadFileAsync(uploadStream, key,
                fileName.EndsWith(".zip") ? "application/zip" : "application/pdf", ct);

            job.ResultFileKey = key;
            job.ResultFileName = fileName;
            job.ResultFileSizeBytes = resultBytes.Length;
            job.Status = "Completed";
            job.ProgressPercent = 100;
            job.CompletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Export job {JobId} failed", exportJobId);
            job.Status = "Failed";
            job.ErrorMessage = ex.Message;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<int> CleanupExpiredExportsAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var expiredJobs = await db.ExportJobs
            .Where(j => j.ExpiresAt < now)
            .ToListAsync(ct);

        var count = 0;
        foreach (var job in expiredJobs)
        {
            if (!string.IsNullOrEmpty(job.ResultFileKey))
            {
                try
                {
                    await fileStorage.DeleteFileAsync(job.ResultFileKey, ct);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete export file {Key} for job {JobId}", job.ResultFileKey, job.Id);
                }
            }

            job.IsDeleted = true;
            job.DeletedAt = now;
            count++;
        }

        if (count > 0)
            await db.SaveChangesAsync(ct);

        return count;
    }

    private async Task GenerateProjectFiles(ExportJob job, List<(string name, byte[] data)> files, CancellationToken ct)
    {
        if (job.TargetEntityId is null) return;
        var projectId = job.TargetEntityId.Value;

        // Project summary
        var summary = await pdfService.GenerateProjectSummaryAsync(projectId, ct);
        files.Add(("prosjekt-sammendrag.pdf", summary));
        job.ProgressPercent = 25;
        await db.SaveChangesAsync(ct);

        // Hours
        var hours = await pdfService.GenerateHoursExportAsync(projectId, null, DateOnly.MinValue, DateOnly.MaxValue, ct);
        files.Add(("timer.pdf", hours));
        job.ProgressPercent = 50;
        await db.SaveChangesAsync(ct);
    }

    private async Task GenerateEmployeeFiles(ExportJob job, List<(string name, byte[] data)> files, CancellationToken ct)
    {
        if (job.TargetEntityId is null) return;
        var personId = job.TargetEntityId.Value;

        var fullCv = await pdfService.GenerateFullCvAsync(personId, ct);
        files.Add(("cv.pdf", fullCv));
        job.ProgressPercent = 100;
    }

    private async Task GenerateCustomFiles(ExportJob job, List<(string name, byte[] data)> files, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(job.SelectionJson)) return;

        var items = JsonSerializer.Deserialize<List<ExportItemSelection>>(job.SelectionJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        var total = items.Count;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            byte[]? pdf = item.Type switch
            {
                "project" => await pdfService.GenerateProjectSummaryAsync(item.Id, ct),
                "deviation" => await pdfService.GenerateDeviationReportAsync(item.Id, ct),
                "checklist" => await pdfService.GenerateChecklistReportAsync(item.Id, ct),
                "sja" => await pdfService.GenerateSjaReportAsync(item.Id, ct),
                "equipment" => await pdfService.GenerateEquipmentReportAsync(item.Id, ct),
                "employee" => await pdfService.GenerateFullCvAsync(item.Id, ct),
                _ => null
            };
            if (pdf is not null)
                files.Add(($"{item.Type}-{item.Id:N}.pdf", pdf));

            job.ProgressPercent = (int)((i + 1) / (double)total * 100);
            await db.SaveChangesAsync(ct);
        }
    }
}
