using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.TaskGroups;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.TaskGroups;

namespace Solodoc.Api.Endpoints;

public static class TaskGroupEndpoints
{
    public static WebApplication MapTaskGroupEndpoints(this WebApplication app)
    {
        app.MapGet("/api/task-groups", ListTaskGroups).RequireAuthorization();
        app.MapPost("/api/task-groups", CreateTaskGroup).RequireAuthorization();
        app.MapGet("/api/task-groups/{id:guid}", GetTaskGroup).RequireAuthorization();
        app.MapPost("/api/task-groups/{id:guid}/roles", AddRole).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListTaskGroups(
        SolodocDbContext db,
        CancellationToken ct)
    {
        var items = await db.TaskGroups
            .OrderBy(tg => tg.Name)
            .Select(tg => new TaskGroupListItemDto(
                tg.Id,
                tg.Name,
                tg.Description))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateTaskGroup(
        CreateTaskGroupRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var taskGroup = new TaskGroup
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            Description = request.Description
        };

        db.TaskGroups.Add(taskGroup);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/task-groups/{taskGroup.Id}", new { id = taskGroup.Id });
    }

    private static async Task<IResult> GetTaskGroup(
        Guid id,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var taskGroup = await db.TaskGroups
            .Where(tg => tg.Id == id)
            .Select(tg => new TaskGroupDetailDto(
                tg.Id,
                tg.Name,
                tg.Description,
                tg.Roles.Select(r => r.RoleName).ToList()))
            .FirstOrDefaultAsync(ct);

        return taskGroup is not null ? Results.Ok(taskGroup) : Results.NotFound();
    }

    private static async Task<IResult> AddRole(
        Guid id,
        AddRoleRequest request,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
            return Results.BadRequest(new { error = "Rollenavn er påkrevd." });

        var taskGroup = await db.TaskGroups.FirstOrDefaultAsync(tg => tg.Id == id, ct);
        if (taskGroup is null)
            return Results.NotFound();

        var role = new TaskGroupRole
        {
            TaskGroupId = id,
            RoleName = request.RoleName
        };

        db.TaskGroupRoles.Add(role);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/task-groups/{id}/roles/{role.Id}", new { id = role.Id });
    }
}

public record AddRoleRequest(string RoleName);
