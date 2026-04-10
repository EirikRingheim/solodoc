using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
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

        project.IsDeleted = true;
        project.DeletedAt = DateTimeOffset.UtcNow;
        project.DeletedBy = personId;

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
