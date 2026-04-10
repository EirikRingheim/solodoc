using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Checklists;
using Solodoc.Domain.Entities.Locations;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Locations;

namespace Solodoc.Api.Endpoints;

public static class LocationEndpoints
{
    public static WebApplication MapLocationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/locations", ListLocations).RequireAuthorization();
        app.MapGet("/api/locations/{id:guid}", GetLocation).RequireAuthorization();
        app.MapPost("/api/locations", CreateLocation).RequireAuthorization();
        app.MapPut("/api/locations/{id:guid}", UpdateLocation).RequireAuthorization();
        app.MapDelete("/api/locations/{id:guid}", DeleteLocation).RequireAuthorization();

        // Template assignments to locations
        app.MapGet("/api/locations/{id:guid}/templates", GetLocationTemplates).RequireAuthorization();
        app.MapPost("/api/locations/{id:guid}/templates", AssignTemplate).RequireAuthorization();
        app.MapDelete("/api/locations/{locationId:guid}/templates/{templateId:guid}", RemoveTemplate).RequireAuthorization();

        // Instances for a location
        app.MapGet("/api/locations/{id:guid}/instances", GetLocationInstances).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListLocations(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var locations = await db.Locations
            .Where(l => l.TenantId == tp.TenantId.Value && l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LocationListItemDto(
                l.Id, l.Name, l.Address, l.Description, l.LocationType,
                db.TemplateAssignments.Count(ta => ta.LocationId == l.Id && ta.TenantId == tp.TenantId.Value),
                db.ChecklistInstances.Count(ci => ci.LocationId == l.Id)))
            .ToListAsync(ct);

        return Results.Ok(locations);
    }

    private static async Task<IResult> GetLocation(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var loc = await db.Locations
            .Where(l => l.Id == id && l.TenantId == tp.TenantId.Value)
            .Select(l => new LocationDetailDto(l.Id, l.Name, l.Address, l.Description, l.LocationType))
            .FirstOrDefaultAsync(ct);

        return loc is not null ? Results.Ok(loc) : Results.NotFound();
    }

    private static async Task<IResult> CreateLocation(
        CreateLocationRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { error = "Navn er pakrevd." });

        var loc = new Location
        {
            TenantId = tp.TenantId.Value,
            Name = req.Name,
            Address = req.Address,
            Description = req.Description,
            LocationType = req.LocationType
        };

        db.Locations.Add(loc);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/locations/{loc.Id}", new { id = loc.Id });
    }

    private static async Task<IResult> UpdateLocation(
        Guid id, CreateLocationRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var loc = await db.Locations.FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tp.TenantId.Value, ct);
        if (loc is null) return Results.NotFound();

        loc.Name = req.Name;
        loc.Address = req.Address;
        loc.Description = req.Description;
        loc.LocationType = req.LocationType;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteLocation(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var loc = await db.Locations.FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tp.TenantId.Value, ct);
        if (loc is null) return Results.NotFound();
        loc.IsActive = false;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetLocationTemplates(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var assignments = await db.TemplateAssignments
            .Where(ta => ta.LocationId == id && ta.TenantId == tp.TenantId.Value)
            .Join(db.ChecklistTemplates, ta => ta.ChecklistTemplateId, t => t.Id,
                (ta, t) => new { t.Id, t.Name, t.Description, t.DocumentType, t.DocumentNumber })
            .ToListAsync(ct);

        return Results.Ok(assignments);
    }

    private static async Task<IResult> AssignTemplate(
        Guid id, AssignTemplateToLocationRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var exists = await db.TemplateAssignments
            .AnyAsync(ta => ta.LocationId == id && ta.ChecklistTemplateId == req.TemplateId && ta.TenantId == tp.TenantId.Value, ct);
        if (exists) return Results.BadRequest(new { error = "Mal er allerede tilordnet denne lokasjonen." });

        db.TemplateAssignments.Add(new TemplateAssignment
        {
            TenantId = tp.TenantId.Value,
            ChecklistTemplateId = req.TemplateId,
            LocationId = id
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> RemoveTemplate(
        Guid locationId, Guid templateId, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var assignment = await db.TemplateAssignments
            .FirstOrDefaultAsync(ta => ta.LocationId == locationId && ta.ChecklistTemplateId == templateId
                && ta.TenantId == tp.TenantId.Value, ct);
        if (assignment is null) return Results.NotFound();
        db.TemplateAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetLocationInstances(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var instances = await db.ChecklistInstances
            .Where(ci => ci.LocationId == id && ci.TenantId == tp.TenantId.Value)
            .OrderByDescending(ci => ci.CreatedAt)
            .Select(ci => new
            {
                ci.Id, ci.TemplateVersionId, ci.Status, ci.CreatedAt,
                ci.LocationIdentifier, ci.StartedById
            })
            .ToListAsync(ct);

        // Resolve names in memory
        var versionIds = instances.Select(i => i.TemplateVersionId).Distinct().ToList();
        var templateNames = versionIds.Count > 0
            ? await db.ChecklistTemplateVersions
                .Where(v => versionIds.Contains(v.Id))
                .Join(db.ChecklistTemplates, v => v.ChecklistTemplateId, t => t.Id,
                    (v, t) => new { v.Id, t.Name })
                .ToDictionaryAsync(x => x.Id, x => x.Name, ct)
            : new Dictionary<Guid, string>();

        var personIds = instances.Select(i => i.StartedById).Distinct().ToList();
        var personNames = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var result = instances.Select(i => new
        {
            i.Id,
            TemplateName = templateNames.TryGetValue(i.TemplateVersionId, out var tn) ? tn : "",
            Status = i.Status.ToString(),
            i.CreatedAt,
            i.LocationIdentifier,
            StartedBy = personNames.TryGetValue(i.StartedById, out var sn) ? sn : ""
        }).ToList();

        return Results.Ok(result);
    }
}
