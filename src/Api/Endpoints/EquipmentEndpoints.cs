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
                e.IsActive))
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
}
