using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Chemicals;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Chemicals;

namespace Solodoc.Api.Endpoints;

public static class ChemicalEndpoints
{
    public static WebApplication MapChemicalEndpoints(this WebApplication app)
    {
        app.MapGet("/api/chemicals", ListChemicals).RequireAuthorization();
        app.MapPost("/api/chemicals", CreateChemical).RequireAuthorization();
        app.MapGet("/api/chemicals/{id:guid}", GetChemical).RequireAuthorization();
        app.MapPost("/api/chemicals/{id:guid}/pictograms", AddPictogram).RequireAuthorization();
        app.MapPost("/api/chemicals/{id:guid}/ppe", AddPpeRequirement).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListChemicals(
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct,
        string? search = null)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var query = db.Chemicals
            .Where(c => c.TenantId == tenantProvider.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Manufacturer != null && c.Manufacturer.ToLower().Contains(term)) ||
                (c.ProductNumber != null && c.ProductNumber.ToLower().Contains(term)));
        }

        var items = await query
            .OrderBy(c => c.Name)
            .Select(c => new ChemicalListItemDto(
                c.Id,
                c.Name,
                c.Manufacturer,
                c.IsActive,
                c.GhsPictograms.Select(g => g.PictogramCode).ToList()))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateChemical(
        CreateChemicalRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var chemical = new Chemical
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            Manufacturer = request.Manufacturer,
            ProductNumber = request.ProductNumber,
            IsActive = true
        };

        db.Chemicals.Add(chemical);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/chemicals/{chemical.Id}", new { id = chemical.Id });
    }

    private static async Task<IResult> GetChemical(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var chemical = await db.Chemicals
            .Where(c => c.Id == id && c.TenantId == tenantProvider.TenantId.Value)
            .Select(c => new ChemicalDetailDto(
                c.Id,
                c.Name,
                c.Manufacturer,
                c.ProductNumber,
                c.IsActive,
                c.GhsPictograms.Select(g => new GhsPictogramDto(g.PictogramCode, g.Description)).ToList(),
                c.PpeRequirements.Select(p => new PpeRequirementDto(p.Requirement, p.IconCode)).ToList()))
            .FirstOrDefaultAsync(ct);

        return chemical is not null ? Results.Ok(chemical) : Results.NotFound();
    }

    private static async Task<IResult> AddPictogram(
        Guid id,
        AddPictogramRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return Results.BadRequest(new { error = "Piktogramkode er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var chemical = await db.Chemicals.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantProvider.TenantId.Value, ct);
        if (chemical is null)
            return Results.NotFound();

        var pictogram = new ChemicalGhsPictogram
        {
            ChemicalId = id,
            PictogramCode = request.Code,
            Description = request.Description
        };

        db.ChemicalGhsPictograms.Add(pictogram);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/chemicals/{id}/pictograms/{pictogram.Id}", new { id = pictogram.Id });
    }

    private static async Task<IResult> AddPpeRequirement(
        Guid id,
        AddPpeRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Requirement))
            return Results.BadRequest(new { error = "Krav er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var chemical = await db.Chemicals.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantProvider.TenantId.Value, ct);
        if (chemical is null)
            return Results.NotFound();

        var ppe = new ChemicalPpeRequirement
        {
            ChemicalId = id,
            Requirement = request.Requirement,
            IconCode = request.IconCode
        };

        db.ChemicalPpeRequirements.Add(ppe);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/chemicals/{id}/ppe/{ppe.Id}", new { id = ppe.Id });
    }
}

public record AddPictogramRequest(string Code, string? Description);
public record AddPpeRequest(string Requirement, string? IconCode);
