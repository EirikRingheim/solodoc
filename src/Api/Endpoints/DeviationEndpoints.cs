using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Deviations;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Deviations;

namespace Solodoc.Api.Endpoints;

public static class DeviationEndpoints
{
    public static WebApplication MapDeviationEndpoints(this WebApplication app)
    {
        app.MapPut("/api/deviations/{id:guid}", UpdateDeviation).RequireAuthorization();
        app.MapPatch("/api/deviations/{id:guid}/assign", AssignDeviation).RequireAuthorization();
        app.MapPatch("/api/deviations/{id:guid}/close", CloseDeviation).RequireAuthorization();
        app.MapPatch("/api/deviations/{id:guid}/reopen", ReopenDeviation).RequireAuthorization();
        app.MapPost("/api/deviations/{id:guid}/comments", AddComment).RequireAuthorization();
        app.MapGet("/api/deviations/{id:guid}/comments", ListComments).RequireAuthorization();
        app.MapPut("/api/deviations/{id:guid}/visibility", SetVisibility).RequireAuthorization();
        app.MapGet("/api/deviations/categories", ListCategories).RequireAuthorization();
        app.MapPost("/api/deviations/categories", CreateCategory).RequireAuthorization();

        return app;
    }

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static async Task<IResult> UpdateDeviation(
        Guid id,
        UpdateDeviationRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "Tittel er påkrevd." });

        var deviation = await db.Deviations.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value, ct);
        if (deviation is null)
            return Results.NotFound();

        deviation.Title = request.Title;
        deviation.Description = request.Description;
        deviation.Severity = request.Severity switch
        {
            "Lav" => DeviationSeverity.Low,
            "Middels" => DeviationSeverity.Medium,
            "Høy" => DeviationSeverity.High,
            _ => deviation.Severity
        };
        deviation.Type = request.Type switch
        {
            "MateriellSkade" => DeviationType.MateriellSkade,
            "Personskade" => DeviationType.Personskade,
            "Nestenulykke" => DeviationType.Nestenulykke,
            "FarligTilstand" => DeviationType.FarligTilstand,
            "Kvalitetsavvik" => DeviationType.Kvalitetsavvik,
            "Miljøavvik" => DeviationType.Miljøavvik,
            null => null,
            _ => deviation.Type
        };
        deviation.CategoryId = request.CategoryId;
        deviation.ProjectId = request.ProjectId;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AssignDeviation(
        Guid id,
        AssignDeviationRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var deviation = await db.Deviations.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value, ct);
        if (deviation is null)
            return Results.NotFound();

        deviation.AssignedToId = request.AssignedToId;
        deviation.CorrectiveActionDeadline = request.Deadline;
        deviation.Status = DeviationStatus.InProgress;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> CloseDeviation(
        Guid id,
        CloseDeviationRequest request,
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

        var deviation = await db.Deviations.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value, ct);
        if (deviation is null)
            return Results.NotFound();

        deviation.Status = DeviationStatus.Closed;
        deviation.ClosedAt = DateTimeOffset.UtcNow;
        deviation.ClosedById = personId.Value;
        deviation.CorrectiveAction = request.CorrectiveAction;
        deviation.CorrectiveActionCompletedAt = request.CompletedAt;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ReopenDeviation(
        Guid id,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var deviation = await db.Deviations.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value, ct);
        if (deviation is null)
            return Results.NotFound();

        deviation.Status = DeviationStatus.InProgress;
        deviation.ClosedAt = null;
        deviation.ClosedById = null;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AddComment(
        Guid id,
        AddCommentRequest request,
        ClaimsPrincipal user,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Text))
            return Results.BadRequest(new { error = "Kommentar kan ikke være tom." });

        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var exists = await db.Deviations.AnyAsync(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value, ct);
        if (!exists)
            return Results.NotFound();

        var comment = new DeviationComment
        {
            DeviationId = id,
            AuthorId = personId.Value,
            Text = request.Text,
            PostedAt = DateTimeOffset.UtcNow
        };

        db.DeviationComments.Add(comment);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/deviations/{id}/comments", new { id = comment.Id });
    }

    private static async Task<IResult> ListComments(
        Guid id,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var exists = await db.Deviations.AnyAsync(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value, ct);
        if (!exists)
            return Results.NotFound();

        var comments = await db.DeviationComments
            .Where(c => c.DeviationId == id)
            .OrderBy(c => c.PostedAt)
            .Select(c => new DeviationCommentDto(
                c.Id,
                db.Persons.Where(p => p.Id == c.AuthorId).Select(p => p.FullName).FirstOrDefault() ?? "",
                c.Text,
                c.PostedAt))
            .ToListAsync(ct);

        return Results.Ok(comments);
    }

    private static async Task<IResult> SetVisibility(
        Guid id,
        SetVisibilityRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var deviation = await db.Deviations.FirstOrDefaultAsync(d => d.Id == id && d.TenantId == tenantProvider.TenantId.Value, ct);
        if (deviation is null)
            return Results.NotFound();

        // Remove existing visibility entries
        var existing = await db.DeviationVisibilities
            .Where(v => v.DeviationId == id)
            .ToListAsync(ct);
        db.DeviationVisibilities.RemoveRange(existing);

        // Add new entries
        if (request.PersonIds.Count > 0)
        {
            foreach (var personId in request.PersonIds)
            {
                db.DeviationVisibilities.Add(new DeviationVisibility
                {
                    DeviationId = id,
                    PersonId = personId
                });
            }
        }

        deviation.IsConfidential = request.PersonIds.Count > 0;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ListCategories(
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var categories = await db.DeviationCategories
            .Where(c => c.TenantId == tenantProvider.TenantId.Value)
            .OrderBy(c => c.Name)
            .Select(c => new DeviationCategoryDto(c.Id, c.Name, c.IsActive))
            .ToListAsync(ct);

        return Results.Ok(categories);
    }

    private static async Task<IResult> CreateCategory(
        CreateCategoryRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var category = new DeviationCategory
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name
        };

        db.DeviationCategories.Add(category);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/deviations/categories/{category.Id}",
            new DeviationCategoryDto(category.Id, category.Name, category.IsActive));
    }
}
