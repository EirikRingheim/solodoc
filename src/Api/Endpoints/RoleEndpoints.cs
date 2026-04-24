using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Auth;

namespace Solodoc.Api.Endpoints;

public static class RoleEndpoints
{
    public static WebApplication MapRoleEndpoints(this WebApplication app)
    {
        app.MapGet("/api/roles", ListRoles).RequireAuthorization();
        app.MapPost("/api/roles", CreateRole).RequireAuthorization();
        app.MapPut("/api/roles/{id:guid}", UpdateRole).RequireAuthorization();
        app.MapDelete("/api/roles/{id:guid}", DeleteRole).RequireAuthorization();
        app.MapGet("/api/roles/permissions", GetPermissionDefinitions).RequireAuthorization();

        return app;
    }

    private static async Task<bool> IsAdmin(ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return false;
        var pid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(pid, out var personId)) return false;
        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == personId && m.TenantId == tp.TenantId.Value && m.State == TenantMembershipState.Active, ct);
        return membership?.Role == TenantRole.TenantAdmin;
    }

    private static async Task<IResult> ListRoles(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (!await IsAdmin(user, db, tp, ct)) return Results.Forbid();
        var tenantId = tp.TenantId.Value;

        var roles = await db.CustomRoles
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.IsSystem ? 0 : 1)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);

        // Count members per role
        var memberCounts = await db.TenantMemberships
            .Where(m => m.TenantId == tenantId && m.State == TenantMembershipState.Active && m.CustomRoleId != null)
            .GroupBy(m => m.CustomRoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var countMap = memberCounts.ToDictionary(x => x.RoleId!.Value, x => x.Count);

        var result = roles.Select(r => new CustomRoleDto(
            r.Id, r.Name, r.Description, r.Color,
            DeserializePermissions(r.Permissions),
            DeserializeList(r.VisibleModules),
            DeserializeFlags(r.FeatureFlagOverrides),
            r.IsSystem,
            countMap.GetValueOrDefault(r.Id, 0)
        )).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> CreateRole(
        CreateCustomRoleRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (!await IsAdmin(user, db, tp, ct)) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        var role = new CustomRole
        {
            TenantId = tp.TenantId.Value,
            Name = request.Name,
            Description = request.Description,
            Color = request.Color,
            Permissions = JsonSerializer.Serialize(request.Permissions ?? []),
            VisibleModules = request.VisibleModules is not null ? JsonSerializer.Serialize(request.VisibleModules) : null,
            FeatureFlagOverrides = request.FeatureFlagOverrides is not null ? JsonSerializer.Serialize(request.FeatureFlagOverrides) : null,
            IsSystem = false
        };

        db.CustomRoles.Add(role);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/roles/{role.Id}", new { id = role.Id });
    }

    private static async Task<IResult> UpdateRole(
        Guid id,
        UpdateCustomRoleRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (!await IsAdmin(user, db, tp, ct)) return Results.Forbid();

        var role = await db.CustomRoles
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tp.TenantId.Value, ct);
        if (role is null) return Results.NotFound();

        role.Name = request.Name;
        role.Description = request.Description;
        role.Color = request.Color;
        role.Permissions = JsonSerializer.Serialize(request.Permissions ?? []);
        role.VisibleModules = request.VisibleModules is not null ? JsonSerializer.Serialize(request.VisibleModules) : null;
        role.FeatureFlagOverrides = request.FeatureFlagOverrides is not null ? JsonSerializer.Serialize(request.FeatureFlagOverrides) : null;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteRole(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (!await IsAdmin(user, db, tp, ct)) return Results.Forbid();

        var role = await db.CustomRoles
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tp.TenantId.Value, ct);
        if (role is null) return Results.NotFound();

        if (role.IsSystem)
            return Results.BadRequest(new { error = "Systemroller kan ikke slettes." });

        // Check if any members use this role
        var memberCount = await db.TenantMemberships
            .CountAsync(m => m.CustomRoleId == id && m.State == TenantMembershipState.Active, ct);
        if (memberCount > 0)
            return Results.BadRequest(new { error = $"Kan ikke slette — {memberCount} ansatte har denne rollen." });

        role.IsDeleted = true;
        role.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    private static IResult GetPermissionDefinitions()
    {
        return Results.Ok(PermissionDefinitions.AllGroups);
    }

    private static List<string> DeserializePermissions(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }

    private static List<string>? DeserializeList(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<List<string>>(json); }
        catch { return null; }
    }

    private static Dictionary<string, bool>? DeserializeFlags(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<Dictionary<string, bool>>(json); }
        catch { return null; }
    }
}
