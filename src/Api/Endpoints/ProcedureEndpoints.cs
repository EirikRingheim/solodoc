using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Procedures;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Checklists;
using Solodoc.Shared.Procedures;

namespace Solodoc.Api.Endpoints;

public static class ProcedureEndpoints
{
    public static WebApplication MapProcedureEndpoints(this WebApplication app)
    {
        app.MapGet("/api/procedures", ListProcedures).RequireAuthorization();
        app.MapGet("/api/procedures/{id:guid}", GetProcedureDetail).RequireAuthorization();
        app.MapPost("/api/procedures", CreateProcedure).RequireAuthorization();
        app.MapPost("/api/procedures/{id:guid}/mark-read", MarkRead).RequireAuthorization();
        app.MapGet("/api/procedures/{id:guid}/read-status", GetReadStatus).RequireAuthorization();

        return app;
    }

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static async Task<IResult> ListProcedures(
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var procedures = await db.ProcedureTemplates
            .Where(p => p.TenantId == tenantProvider.TenantId.Value && !p.IsArchived)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProcedureListItemDto(
                p.Id,
                p.Name,
                p.Description,
                p.Category,
                db.ProcedureBlocks.Count(b => b.ProcedureTemplateId == p.Id)))
            .ToListAsync(ct);

        return Results.Ok(procedures);
    }

    private static async Task<IResult> GetProcedureDetail(
        Guid id,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var procedure = await db.ProcedureTemplates
            .Where(p => p.Id == id && p.TenantId == tenantProvider.TenantId.Value)
            .Select(p => new ProcedureDetailDto(
                p.Id,
                p.Name,
                p.Description,
                p.Category,
                p.IsPublished,
                db.ProcedureBlocks
                    .Where(b => b.ProcedureTemplateId == p.Id)
                    .OrderBy(b => b.SortOrder)
                    .Select(b => new ProcedureBlockDto(b.Id, b.Type, b.Content, b.SortOrder, b.ImageFileKey, b.Caption))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return procedure is not null ? Results.Ok(procedure) : Results.NotFound();
    }

    private static async Task<IResult> CreateProcedure(
        CreateProcedureRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var procedure = new ProcedureTemplate
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            CurrentVersion = 1,
            IsPublished = false,
            IsArchived = false
        };

        db.ProcedureTemplates.Add(procedure);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/procedures/{procedure.Id}", new { id = procedure.Id });
    }

    private static async Task<IResult> MarkRead(
        Guid id,
        ClaimsPrincipal user,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var procedure = await db.ProcedureTemplates
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantProvider.TenantId.Value, ct);

        if (procedure is null)
            return Results.NotFound();

        // Check if already read
        var alreadyRead = await db.ProcedureReadConfirmations
            .AnyAsync(r => r.ProcedureTemplateId == id && r.PersonId == personId.Value, ct);

        if (alreadyRead)
            return Results.Ok(new { message = "Allerede markert som lest." });

        var confirmation = new ProcedureReadConfirmation
        {
            ProcedureTemplateId = id,
            PersonId = personId.Value,
            ReadAt = DateTimeOffset.UtcNow
        };

        db.ProcedureReadConfirmations.Add(confirmation);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { message = "Markert som lest." });
    }

    private static async Task<IResult> GetReadStatus(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var procedure = await db.ProcedureTemplates
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantProvider.TenantId.Value, ct);

        if (procedure is null)
            return Results.NotFound();

        // Count total workers in the tenant
        var totalWorkers = await db.TenantMemberships
            .CountAsync(m => m.TenantId == tenantProvider.TenantId.Value, ct);

        // Get readers
        var readers = await db.ProcedureReadConfirmations
            .Where(r => r.ProcedureTemplateId == id)
            .Join(db.Persons,
                r => r.PersonId,
                p => p.Id,
                (r, p) => new ProcedureReaderDto(p.FullName, r.ReadAt))
            .OrderByDescending(r => r.ReadAt)
            .ToListAsync(ct);

        return Results.Ok(new ProcedureReadStatusDto(totalWorkers, readers.Count, readers));
    }
}
