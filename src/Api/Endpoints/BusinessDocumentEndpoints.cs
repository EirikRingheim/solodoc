using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Documents;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Documents;

namespace Solodoc.Api.Endpoints;

public static class BusinessDocumentEndpoints
{
    public static WebApplication MapBusinessDocumentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/business-documents", ListDocuments).RequireAuthorization();
        app.MapPost("/api/business-documents", CreateDocument).RequireAuthorization();
        app.MapGet("/api/business-documents/{id:guid}", GetDocument).RequireAuthorization();
        app.MapPut("/api/business-documents/{id:guid}", UpdateDocument).RequireAuthorization();
        app.MapDelete("/api/business-documents/{id:guid}", DeleteDocument).RequireAuthorization();

        // Waste disposal entries
        app.MapGet("/api/business-documents/{id:guid}/waste-entries", GetWasteEntries).RequireAuthorization();
        app.MapPost("/api/business-documents/{id:guid}/waste-entries", AddWasteEntry).RequireAuthorization();

        return app;
    }

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static async Task<IResult> ListDocuments(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        string? type = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var query = db.BusinessDocuments.Where(d => d.TenantId == tp.TenantId.Value);
        if (!string.IsNullOrEmpty(type) && Enum.TryParse<BusinessDocumentType>(type, true, out var dt))
            query = query.Where(d => d.DocumentType == dt);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new BusinessDocumentDto(
                d.Id, d.DocumentType.ToString(), d.Title,
                d.ProjectId,
                d.ProjectId != null ? db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Name).FirstOrDefault() : null,
                d.Status, d.CreatedAt, d.GeneratedPdfKey))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateDocument(
        CreateBusinessDocumentRequest request,
        ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var personId = GetPersonId(user);
        if (personId is null) return Results.Unauthorized();

        if (!Enum.TryParse<BusinessDocumentType>(request.DocumentType, true, out var docType))
            return Results.BadRequest(new { error = "Ugyldig dokumenttype." });

        var doc = new BusinessDocument
        {
            TenantId = tp.TenantId.Value,
            DocumentType = docType,
            Title = request.Title,
            ProjectId = request.ProjectId,
            ContentJson = request.ContentJson,
            CreatedById = personId.Value,
            Status = "Draft"
        };

        db.BusinessDocuments.Add(doc);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/business-documents/{doc.Id}", new { id = doc.Id });
    }

    private static async Task<IResult> GetDocument(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var doc = await db.BusinessDocuments
            .Where(d => d.Id == id && d.TenantId == tp.TenantId.Value)
            .Select(d => new
            {
                d.Id, DocumentType = d.DocumentType.ToString(), d.Title, d.ProjectId,
                ProjectName = d.ProjectId != null ? db.Projects.Where(p => p.Id == d.ProjectId).Select(p => p.Name).FirstOrDefault() : null,
                d.Status, d.ContentJson, d.CreatedAt, d.GeneratedPdfKey
            })
            .FirstOrDefaultAsync(ct);
        return doc is not null ? Results.Ok(doc) : Results.NotFound();
    }

    private static async Task<IResult> UpdateDocument(
        Guid id, CreateBusinessDocumentRequest request,
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var doc = await db.BusinessDocuments.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tp.TenantId.Value, ct);
        if (doc is null) return Results.NotFound();

        doc.Title = request.Title;
        doc.ProjectId = request.ProjectId;
        doc.ContentJson = request.ContentJson;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteDocument(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var doc = await db.BusinessDocuments.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tp.TenantId.Value, ct);
        if (doc is null) return Results.NotFound();
        doc.IsDeleted = true;
        doc.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Waste entries ──

    private static async Task<IResult> GetWasteEntries(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var entries = await db.WasteDisposalEntries
            .Where(e => e.BusinessDocumentId == id)
            .OrderByDescending(e => e.DisposedAt)
            .Select(e => new WasteDisposalEntryDto(
                e.Id, e.Category.ToString(), e.Description, e.WeightKg,
                e.DisposedAt, e.DisposalMethod, e.ReceiptFileKey))
            .ToListAsync(ct);
        return Results.Ok(entries);
    }

    private static async Task<IResult> AddWasteEntry(
        Guid id, CreateWasteDisposalEntryRequest request,
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        if (!Enum.TryParse<WasteCategory>(request.Category, true, out var cat))
            return Results.BadRequest(new { error = "Ugyldig avfallskategori." });

        db.WasteDisposalEntries.Add(new WasteDisposalEntry
        {
            BusinessDocumentId = id,
            Category = cat,
            Description = request.Description,
            WeightKg = request.WeightKg,
            DisposedAt = request.DisposedAt,
            DisposalMethod = request.DisposalMethod
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
