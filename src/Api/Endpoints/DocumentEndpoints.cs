using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Documents;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Documents;

namespace Solodoc.Api.Endpoints;

public static class DocumentEndpoints
{
    public static WebApplication MapDocumentEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/documents").RequireAuthorization();

        g.MapGet("/folders", ListFolders);
        g.MapPost("/folders", CreateFolder);
        g.MapPut("/folders/{id:guid}", UpdateFolder);
        g.MapDelete("/folders/{id:guid}", DeleteFolder);
        g.MapGet("/", ListDocuments);
        g.MapPost("/upload", UploadDocument).DisableAntiforgery();
        g.MapGet("/{id:guid}/download", DownloadDocument);
        g.MapDelete("/{id:guid}", DeleteDocument);

        return app;
    }

    private static readonly HashSet<string> AllowedExtensions =
        [".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".heic",
         ".doc", ".docx", ".xlsx", ".xls", ".pptx", ".dwg", ".dxf", ".zip", ".txt", ".csv"];

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var c = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(c, out var pid) ? pid : null;
    }

    private static async Task<TenantRole?> GetRole(Guid personId, Guid tenantId, SolodocDbContext db, CancellationToken ct)
    {
        var m = await db.TenantMemberships.FirstOrDefaultAsync(
            m => m.PersonId == personId && m.TenantId == tenantId && m.State == TenantMembershipState.Active, ct);
        return m?.Role;
    }

    private static bool CanUpload(TenantRole? role, UploadPermission permission) => permission switch
    {
        UploadPermission.Everyone => true,
        UploadPermission.AdminAndProjectLeader => role is TenantRole.TenantAdmin or TenantRole.ProjectLeader,
        UploadPermission.AdminOnly => role is TenantRole.TenantAdmin,
        _ => false
    };

    // ── Folders ──

    private static async Task<IResult> ListFolders(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        Guid? projectId = null, Guid? parentId = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var query = db.DocumentFolders.Where(f => f.TenantId == tp.TenantId.Value);
        if (projectId.HasValue)
            query = query.Where(f => f.ProjectId == projectId.Value);
        else
            query = query.Where(f => f.ProjectId == null);

        if (parentId.HasValue)
            query = query.Where(f => f.ParentFolderId == parentId.Value);
        else
            query = query.Where(f => f.ParentFolderId == null);

        var folders = await query.OrderBy(f => f.Name)
            .Select(f => new DocumentFolderDto(
                f.Id, f.Name, f.Description, f.ParentFolderId, f.ProjectId,
                f.UploadPermission.ToString(), f.IsSystemFolder,
                db.Documents.Count(d => d.FolderId == f.Id && !d.IsDeleted),
                db.DocumentFolders.Count(sf => sf.ParentFolderId == f.Id && !sf.IsDeleted)))
            .ToListAsync(ct);

        return Results.Ok(folders);
    }

    private static async Task<IResult> CreateFolder(
        CreateFolderRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var pid = GetPersonId(user);
        if (pid is null) return Results.Unauthorized();

        var role = await GetRole(pid.Value, tp.TenantId.Value, db, ct);
        if (role is not TenantRole.TenantAdmin and not TenantRole.ProjectLeader)
            return Results.Forbid();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Mappenavn er påkrevd." });

        var perm = Enum.TryParse<UploadPermission>(request.UploadPermission, true, out var p)
            ? p : UploadPermission.AdminAndProjectLeader;

        var folder = new DocumentFolder
        {
            TenantId = tp.TenantId.Value,
            Name = request.Name.Trim(),
            Description = request.Description,
            ParentFolderId = request.ParentFolderId,
            ProjectId = request.ProjectId,
            UploadPermission = perm
        };

        db.DocumentFolders.Add(folder);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/documents/folders/{folder.Id}", new { id = folder.Id });
    }

    private static async Task<IResult> UpdateFolder(
        Guid id, CreateFolderRequest request,
        ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var folder = await db.DocumentFolders.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tp.TenantId.Value, ct);
        if (folder is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name)) folder.Name = request.Name.Trim();
        if (request.Description is not null) folder.Description = request.Description;
        if (Enum.TryParse<UploadPermission>(request.UploadPermission, true, out var p))
            folder.UploadPermission = p;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteFolder(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var folder = await db.DocumentFolders.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tp.TenantId.Value, ct);
        if (folder is null) return Results.NotFound();
        if (folder.IsSystemFolder)
            return Results.BadRequest(new { error = "Systemmapper kan ikke slettes." });

        var hasContent = await db.Documents.AnyAsync(d => d.FolderId == id && !d.IsDeleted, ct)
            || await db.DocumentFolders.AnyAsync(f => f.ParentFolderId == id && !f.IsDeleted, ct);
        if (hasContent)
            return Results.BadRequest(new { error = "Mappen er ikke tom. Slett innholdet først." });

        folder.IsDeleted = true;
        folder.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Documents ──

    private static async Task<IResult> ListDocuments(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        Guid? folderId = null, string? search = null, string? sortBy = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var query = db.Documents.Where(d => d.TenantId == tp.TenantId.Value);
        if (folderId.HasValue)
            query = query.Where(d => d.FolderId == folderId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(d => d.Name.ToLower().Contains(term) || (d.Description != null && d.Description.ToLower().Contains(term)));
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => query.OrderBy(d => d.Name),
            "size" => query.OrderByDescending(d => d.FileSize),
            "type" => query.OrderBy(d => d.ContentType),
            _ => query.OrderByDescending(d => d.CreatedAt)
        };

        var docs = await query.Take(200)
            .Select(d => new DocumentDto(
                d.Id, d.Name, d.Description, d.FileKey, d.FileSize, d.ContentType,
                db.Persons.Where(p => p.Id == d.UploadedById).Select(p => p.FullName).FirstOrDefault() ?? "",
                d.Category, d.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(docs);
    }

    private static async Task<IResult> UploadDocument(
        HttpRequest request,
        SolodocDbContext db,
        ITenantProvider tp,
        IFileStorageService fileStorage,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var pid = GetPersonId(user);
        if (pid is null) return Results.Unauthorized();

        if (!request.HasFormContentType) return Results.BadRequest(new { error = "Forventet multipart/form-data." });

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.FirstOrDefault();
        if (file is null || file.Length == 0) return Results.BadRequest(new { error = "Ingen fil mottatt." });
        if (file.Length > 25 * 1024 * 1024) return Results.BadRequest(new { error = "Filen er for stor. Maks 25MB." });

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        if (!AllowedExtensions.Contains(ext))
            return Results.BadRequest(new { error = $"Filtypen '{ext}' er ikke tillatt." });

        var folderIdStr = form["folderId"].FirstOrDefault();
        if (!Guid.TryParse(folderIdStr, out var folderId))
            return Results.BadRequest(new { error = "Mappe er påkrevd." });

        // Check folder permissions
        var folder = await db.DocumentFolders.FirstOrDefaultAsync(f => f.Id == folderId && f.TenantId == tp.TenantId.Value, ct);
        if (folder is null) return Results.NotFound();

        var role = await GetRole(pid.Value, tp.TenantId.Value, db, ct);
        if (!CanUpload(role, folder.UploadPermission))
            return Results.Forbid();

        var tenantId = tp.TenantId.Value;
        var key = $"{tenantId}/documents/{Guid.NewGuid()}{ext}";

        using var stream = file.OpenReadStream();
        await fileStorage.UploadFileAsync(stream, key, file.ContentType, ct);

        var docName = form["name"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(docName)) docName = file.FileName;

        var doc = new Document
        {
            TenantId = tenantId,
            FolderId = folderId,
            Name = docName,
            Description = form["description"].FirstOrDefault(),
            FileKey = key,
            FileSize = file.Length,
            ContentType = file.ContentType,
            UploadedById = pid.Value,
            Category = form["category"].FirstOrDefault()
        };

        db.Documents.Add(doc);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/documents/{doc.Id}", new { id = doc.Id, key });
    }

    private static async Task<IResult> DownloadDocument(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tp,
        IFileStorageService fileStorage,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var doc = await db.Documents.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tp.TenantId.Value, ct);
        if (doc is null) return Results.NotFound();

        var stream = await fileStorage.DownloadFileAsync(doc.FileKey, ct);
        return Results.File(stream, doc.ContentType, doc.Name);
    }

    private static async Task<IResult> DeleteDocument(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var pid = GetPersonId(user);
        if (pid is null) return Results.Unauthorized();

        var doc = await db.Documents.Include(d => d.Folder).FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tp.TenantId.Value, ct);
        if (doc is null) return Results.NotFound();

        var role = await GetRole(pid.Value, tp.TenantId.Value, db, ct);
        if (!CanUpload(role, doc.Folder.UploadPermission))
            return Results.Forbid();

        doc.IsDeleted = true;
        doc.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
