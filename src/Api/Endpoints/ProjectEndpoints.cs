using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Domain.Entities.Checklists;
using Solodoc.Shared.Projects;

namespace Solodoc.Api.Endpoints;

public static class ProjectEndpoints
{
    public static WebApplication MapProjectEndpoints(this WebApplication app)
    {
        app.MapPost("/api/projects", CreateProject).RequireAuthorization();
        app.MapPut("/api/projects/{id:guid}", UpdateProject).RequireAuthorization();
        app.MapPatch("/api/projects/{id:guid}/status", ChangeProjectStatus).RequireAuthorization();
        app.MapDelete("/api/projects/{id:guid}", DeleteProject).RequireAuthorization();
        app.MapGet("/api/projects/{id:guid}/subprojects", GetSubProjects).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> CreateProject(
        CreateProjectRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        // Validate parent project (one-level enforcement)
        if (request.ParentProjectId.HasValue)
        {
            var parent = await db.Projects
                .FirstOrDefaultAsync(p => p.Id == request.ParentProjectId.Value && p.TenantId == tenantProvider.TenantId.Value, ct);
            if (parent is null)
                return Results.BadRequest(new { error = "Hovedprosjekt ikke funnet." });
            if (parent.ParentProjectId.HasValue)
                return Results.BadRequest(new { error = "Underprosjekter kan ikke ha egne underprosjekter." });
        }

        var project = new Project
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            Description = request.Description,
            CustomerId = request.CustomerId,
            ClientName = request.ClientName,
            Address = request.Address,
            StartDate = request.StartDate,
            PlannedEndDate = request.PlannedEndDate,
            EstimatedHours = request.EstimatedHours,
            ParentProjectId = request.ParentProjectId,
            Status = ProjectStatus.Planlagt
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/projects/{project.Id}", new { id = project.Id });
    }

    private static async Task<IResult> UpdateProject(
        Guid id,
        UpdateProjectRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantProvider.TenantId.Value, ct);
        if (project is null)
            return Results.NotFound();

        project.Name = request.Name;
        project.Description = request.Description;
        project.CustomerId = request.CustomerId;
        project.ClientName = request.ClientName;
        project.Address = request.Address;
        project.StartDate = request.StartDate;
        project.PlannedEndDate = request.PlannedEndDate;
        project.EstimatedHours = request.EstimatedHours;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ChangeProjectStatus(
        Guid id,
        ChangeStatusRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantProvider.TenantId.Value, ct);
        if (project is null)
            return Results.NotFound();

        var status = request.Status switch
        {
            "Planlagt" => ProjectStatus.Planlagt,
            "Aktiv" => ProjectStatus.Active,
            "Fullført" => ProjectStatus.Completed,
            "Kansellert" => ProjectStatus.Cancelled,
            _ => (ProjectStatus?)null
        };

        if (status is null)
            return Results.BadRequest(new { error = $"Ugyldig status: {request.Status}" });

        project.Status = status.Value;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteProject(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantProvider.TenantId.Value, ct);
        if (project is null)
            return Results.NotFound();

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        Guid.TryParse(personIdClaim, out var personId);

        // Block deletion if project has active sub-projects
        var hasChildren = await db.Projects.AnyAsync(p => p.ParentProjectId == id && !p.IsDeleted, ct);
        if (hasChildren)
            return Results.BadRequest(new { error = "Kan ikke slette prosjekt med aktive underprosjekter. Slett underprosjektene forst." });

        project.IsDeleted = true;
        project.DeletedAt = DateTimeOffset.UtcNow;
        project.DeletedBy = personId;

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> GetSubProjects(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var subs = await db.Projects
            .Where(p => p.ParentProjectId == id && p.TenantId == tenantProvider.TenantId.Value)
            .OrderBy(p => p.Name)
            .Select(p => new SubProjectSummaryDto(
                p.Id, p.Name,
                p.Status == ProjectStatus.Active ? "Aktiv"
                    : p.Status == ProjectStatus.Completed ? "Fullfort"
                    : p.Status == ProjectStatus.Planlagt ? "Planlagt"
                    : "Kansellert",
                db.TimeEntries.Where(t => t.ProjectId == p.Id && !t.IsDeleted).Sum(t => t.Hours),
                db.Deviations.Count(d => d.ProjectId == p.Id && !d.IsDeleted && d.Status != DeviationStatus.Closed),
                db.ChecklistInstances.Count(c => c.ProjectId == p.Id && !c.IsDeleted && (c.Status == ChecklistInstanceStatus.Submitted || c.Status == ChecklistInstanceStatus.Approved)),
                db.ChecklistInstances.Count(c => c.ProjectId == p.Id && !c.IsDeleted)))
            .ToListAsync(ct);

        return Results.Ok(subs);
    }
}
