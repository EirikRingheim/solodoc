using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Equipment;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Equipment;

namespace Solodoc.Api.Endpoints;

public static class EquipmentEndpoints
{
    public static WebApplication MapEquipmentEndpoints(this WebApplication app)
    {
        app.MapGet("/api/equipment", ListEquipment).RequireAuthorization();
        app.MapPost("/api/equipment", CreateEquipment).RequireAuthorization();
        app.MapGet("/api/equipment/{id:guid}", GetEquipmentDetail).RequireAuthorization();
        app.MapPut("/api/equipment/{id:guid}", UpdateEquipment).RequireAuthorization();
        app.MapPost("/api/equipment/{id:guid}/maintenance", AddMaintenance).RequireAuthorization();
        app.MapGet("/api/equipment/by-project/{projectId:guid}", GetByProject).RequireAuthorization();
        app.MapGet("/api/equipment/by-job/{jobId:guid}", GetByJob).RequireAuthorization();
        app.MapGet("/api/equipment/by-location/{locationId:guid}", GetByLocation).RequireAuthorization();
        app.MapPost("/api/equipment/{id:guid}/location", UpdateLocation).RequireAuthorization();
        app.MapPost("/api/equipment/{id:guid}/assign", AssignToProject).RequireAuthorization();
        app.MapDelete("/api/equipment/{id:guid}/assign/{assignmentId:guid}", RemoveAssignment).RequireAuthorization();

        // Equipment type categories
        app.MapGet("/api/equipment/type-categories", ListTypeCategories).RequireAuthorization();
        app.MapPost("/api/equipment/type-categories", CreateTypeCategory).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListEquipment(
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct,
        string? search = null)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var query = db.Equipment
            .Where(e => e.TenantId == tenantProvider.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(e =>
                e.Name.ToLower().Contains(term) ||
                (e.Type != null && e.Type.ToLower().Contains(term)) ||
                (e.RegistrationNumber != null && e.RegistrationNumber.ToLower().Contains(term)) ||
                (e.Make != null && e.Make.ToLower().Contains(term)) ||
                (e.Model != null && e.Model.ToLower().Contains(term)));
        }

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new EquipmentListItemDto(
                e.Id,
                e.Name,
                e.Type,
                e.RegistrationNumber,
                e.Make,
                e.Model,
                e.IsActive,
                e.CurrentProjectId != null
                    ? db.Projects.Where(p => p.Id == e.CurrentProjectId).Select(p => p.Name).FirstOrDefault()
                    : null,
                e.LocationDescription))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateEquipment(
        CreateEquipmentRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var equipment = new Domain.Entities.Equipment.Equipment
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            Type = request.Type,
            RegistrationNumber = request.RegistrationNumber,
            SerialNumber = request.SerialNumber,
            Year = request.Year,
            Make = request.Make,
            Model = request.Model,
            IsActive = true
        };

        db.Equipment.Add(equipment);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/equipment/{equipment.Id}", new { id = equipment.Id });
    }

    private static async Task<IResult> GetEquipmentDetail(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var equipment = await db.Equipment
            .Where(e => e.Id == id && e.TenantId == tenantProvider.TenantId.Value)
            .Select(e => new EquipmentDetailDto(
                e.Id,
                e.Name,
                e.Type,
                e.RegistrationNumber,
                e.SerialNumber,
                e.Year,
                e.Make,
                e.Model,
                e.IsActive,
                e.Latitude,
                e.Longitude,
                e.LocationDescription,
                e.CurrentProjectId,
                e.CurrentProjectId != null
                    ? db.Projects.Where(p => p.Id == e.CurrentProjectId).Select(p => p.Name).FirstOrDefault()
                    : null,
                db.EquipmentMaintenanceRecords
                    .Where(m => m.EquipmentId == e.Id)
                    .OrderByDescending(m => m.Date)
                    .Select(m => new MaintenanceLogDto(
                        m.Id,
                        m.Description,
                        m.Date,
                        m.PerformedById != null
                            ? db.Persons.Where(p => p.Id == m.PerformedById).Select(p => p.FullName).FirstOrDefault()
                            : null,
                        m.Cost))
                    .ToList(),
                db.EquipmentProjectAssignments
                    .Where(a => a.EquipmentId == e.Id)
                    .OrderByDescending(a => a.AssignedFrom)
                    .Select(a => new EquipmentAssignmentDto(
                        a.Id,
                        a.ProjectId,
                        db.Projects.Where(p => p.Id == a.ProjectId).Select(p => p.Name).FirstOrDefault() ?? "",
                        a.AssignedFrom,
                        a.AssignedTo))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return equipment is not null ? Results.Ok(equipment) : Results.NotFound();
    }

    private static async Task<IResult> UpdateEquipment(
        Guid id,
        CreateEquipmentRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var equipment = await db.Equipment.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantProvider.TenantId.Value, ct);
        if (equipment is null)
            return Results.NotFound();

        equipment.Name = request.Name;
        equipment.Type = request.Type;
        equipment.RegistrationNumber = request.RegistrationNumber;
        equipment.SerialNumber = request.SerialNumber;
        equipment.Year = request.Year;
        equipment.Make = request.Make;
        equipment.Model = request.Model;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AddMaintenance(
        Guid id,
        AddMaintenanceRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest(new { error = "Beskrivelse er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var exists = await db.Equipment.AnyAsync(e => e.Id == id && e.TenantId == tenantProvider.TenantId.Value, ct);
        if (!exists)
            return Results.NotFound();

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        Guid.TryParse(personIdClaim, out var personId);

        var maintenance = new EquipmentMaintenance
        {
            EquipmentId = id,
            Description = request.Description,
            Date = request.Date,
            PerformedById = personId != Guid.Empty ? personId : null,
            Cost = request.Cost,
            Notes = request.Notes
        };

        db.EquipmentMaintenanceRecords.Add(maintenance);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/equipment/{id}/maintenance/{maintenance.Id}", new { id = maintenance.Id });
    }

    private static async Task<IResult> GetByProject(
        Guid projectId, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var items = await db.Equipment
            .Where(e => e.TenantId == tp.TenantId.Value && e.CurrentProjectId == projectId)
            .OrderBy(e => e.Name)
            .Select(e => new EquipmentListItemDto(
                e.Id, e.Name, e.Type, e.RegistrationNumber, e.Make, e.Model, e.IsActive,
                db.Projects.Where(p => p.Id == e.CurrentProjectId).Select(p => p.Name).FirstOrDefault(),
                e.LocationDescription))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> GetByJob(
        Guid jobId, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var items = await db.Equipment
            .Where(e => e.TenantId == tp.TenantId.Value && e.CurrentJobId == jobId)
            .OrderBy(e => e.Name)
            .Select(e => new EquipmentListItemDto(e.Id, e.Name, e.Type, e.RegistrationNumber, e.Make, e.Model, e.IsActive, null, e.LocationDescription))
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> GetByLocation(
        Guid locationId, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var items = await db.Equipment
            .Where(e => e.TenantId == tp.TenantId.Value && e.CurrentLocationId == locationId)
            .OrderBy(e => e.Name)
            .Select(e => new EquipmentListItemDto(e.Id, e.Name, e.Type, e.RegistrationNumber, e.Make, e.Model, e.IsActive, null, e.LocationDescription))
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    // ── Location & Assignment ──

    private static void ClearEquipmentLocation(Domain.Entities.Equipment.Equipment eq)
    {
        eq.CurrentProjectId = null;
        eq.CurrentJobId = null;
        eq.CurrentLocationId = null;
        eq.Latitude = null;
        eq.Longitude = null;
        eq.LocationDescription = null;
    }

    private static async Task<IResult> UpdateLocation(
        Guid id, UpdateEquipmentLocationRequest request,
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var eq = await db.Equipment.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tp.TenantId.Value, ct);
        if (eq is null) return Results.NotFound();

        ClearEquipmentLocation(eq);
        eq.CurrentProjectId = request.CurrentProjectId;
        eq.CurrentJobId = request.CurrentJobId;
        eq.CurrentLocationId = request.CurrentLocationId;
        eq.Latitude = request.Latitude;
        eq.Longitude = request.Longitude;
        eq.LocationDescription = request.LocationDescription;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AssignToProject(
        Guid id, AssignEquipmentToProjectRequest request,
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var eq = await db.Equipment.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tp.TenantId.Value, ct);
        if (eq is null) return Results.NotFound();

        var assignment = new EquipmentProjectAssignment
        {
            EquipmentId = id,
            ProjectId = request.ProjectId,
            AssignedFrom = request.AssignedFrom,
            AssignedTo = request.AssignedTo
        };
        db.EquipmentProjectAssignments.Add(assignment);

        ClearEquipmentLocation(eq);
        eq.CurrentProjectId = request.ProjectId;
        var projName = await db.Projects.Where(p => p.Id == request.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(ct);
        eq.LocationDescription = projName;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = assignment.Id });
    }

    private static async Task<IResult> RemoveAssignment(
        Guid id, Guid assignmentId,
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var assignment = await db.EquipmentProjectAssignments
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.EquipmentId == id, ct);
        if (assignment is null) return Results.NotFound();

        db.EquipmentProjectAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Type Categories ──

    private static async Task<IResult> ListTypeCategories(
        ITenantProvider tp, SolodocDbContext db, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var categories = await db.EquipmentTypeCategories
            .Where(c => c.TenantId == tp.TenantId.Value && c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new EquipmentTypeCategoryDto(c.Id, c.Name, c.IsActive, c.SortOrder, c.IsDefault))
            .ToListAsync(ct);

        return Results.Ok(categories);
    }

    private static async Task<IResult> CreateTypeCategory(
        CreateEquipmentTypeCategoryRequest request,
        ITenantProvider tp, SolodocDbContext db, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        var exists = await db.EquipmentTypeCategories
            .AnyAsync(c => c.TenantId == tp.TenantId.Value && c.Name == request.Name, ct);
        if (exists) return Results.Conflict(new { error = "Kategori finnes allerede." });

        var maxSort = await db.EquipmentTypeCategories
            .Where(c => c.TenantId == tp.TenantId.Value)
            .MaxAsync(c => (int?)c.SortOrder, ct) ?? 0;

        db.EquipmentTypeCategories.Add(new EquipmentTypeCategory
        {
            TenantId = tp.TenantId.Value,
            Name = request.Name.Trim(),
            SortOrder = maxSort + 1,
            IsActive = true,
            IsDefault = false
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
