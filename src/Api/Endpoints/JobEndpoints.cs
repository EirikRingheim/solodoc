using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Common;
using Solodoc.Shared.Projects;

namespace Solodoc.Api.Endpoints;

public static class JobEndpoints
{
    public static WebApplication MapJobEndpoints(this WebApplication app)
    {
        app.MapGet("/api/jobs", ListJobs).RequireAuthorization();
        app.MapGet("/api/jobs/{id:guid}", GetJob).RequireAuthorization();
        app.MapPost("/api/jobs", CreateJob).RequireAuthorization();
        app.MapPut("/api/jobs/{id:guid}", UpdateJob).RequireAuthorization();
        app.MapPatch("/api/jobs/{id:guid}/status", ChangeJobStatus).RequireAuthorization();
        app.MapPost("/api/jobs/{id:guid}/parts", AddPartsItem).RequireAuthorization();
        app.MapPut("/api/jobs/{id:guid}/parts/{partId:guid}", UpdatePartsItem).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListJobs(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct,
        int page = 1,
        int pageSize = 10,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = false)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId))
            return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
            .FirstOrDefaultAsync(ct);

        if (membership is null)
            return Results.Ok(new PagedResult<JobListItemDto>([], 0, page, pageSize));

        var tenantId = membership.TenantId;

        var query = db.Jobs.Where(j => j.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(j =>
                j.Description.ToLower().Contains(term) ||
                (j.Customer != null && j.Customer.Name.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);

        query = sortBy?.ToLowerInvariant() switch
        {
            "description" => sortDesc ? query.OrderByDescending(j => j.Description) : query.OrderBy(j => j.Description),
            "status" => sortDesc ? query.OrderByDescending(j => j.Status) : query.OrderBy(j => j.Status),
            "customer" => sortDesc
                ? query.OrderByDescending(j => j.Customer != null ? j.Customer.Name : null)
                : query.OrderBy(j => j.Customer != null ? j.Customer.Name : null),
            "createdat" => sortDesc ? query.OrderByDescending(j => j.CreatedAt) : query.OrderBy(j => j.CreatedAt),
            _ => query.OrderByDescending(j => j.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new JobListItemDto(
                j.Id,
                j.Description,
                j.Status == JobStatus.Active ? "Aktiv"
                    : j.Status == JobStatus.WaitingForParts ? "Venter på deler"
                    : "Fullført",
                j.Customer != null ? j.Customer.Name : null,
                db.Persons.Where(p => p.Id == j.CreatedById).Select(p => p.FullName).FirstOrDefault() ?? "",
                j.CreatedAt,
                db.TimeEntries.Where(t => t.JobId == j.Id).Sum(t => t.Hours)))
            .ToListAsync(ct);

        return Results.Ok(new PagedResult<JobListItemDto>(items, totalCount, page, pageSize));
    }

    private static async Task<IResult> GetJob(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var job = await db.Jobs
            .Where(j => j.Id == id && j.TenantId == tenantProvider.TenantId.Value)
            .Select(j => new JobDetailDto(
                j.Id,
                j.Description,
                j.Status == JobStatus.Active ? "Aktiv"
                    : j.Status == JobStatus.WaitingForParts ? "Venter på deler"
                    : "Fullført",
                j.Customer != null ? j.Customer.Name : null,
                j.Address,
                j.Notes,
                db.Persons.Where(p => p.Id == j.CreatedById).Select(p => p.FullName).FirstOrDefault() ?? "",
                j.CreatedAt,
                j.PartsItems
                    .Select(pi => new JobPartsItemDto(
                        pi.Id,
                        pi.Description,
                        pi.Status == PartsItemStatus.Trengs ? "Trengs"
                            : pi.Status == PartsItemStatus.Bestilt ? "Bestilt"
                            : "Mottatt",
                        pi.Notes))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return job is not null ? Results.Ok(job) : Results.NotFound();
    }

    private static async Task<IResult> CreateJob(
        CreateJobRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest(new { error = "Beskrivelse er påkrevd." });

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId) || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var job = new Job
        {
            TenantId = tenantProvider.TenantId.Value,
            Description = request.Description,
            CustomerId = request.CustomerId,
            Address = request.Address,
            Notes = request.Notes,
            CreatedById = personId,
            Status = JobStatus.Active
        };

        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/jobs/{job.Id}", new { id = job.Id });
    }

    private static async Task<IResult> UpdateJob(
        Guid id,
        UpdateJobRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest(new { error = "Beskrivelse er påkrevd." });

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantProvider.TenantId.Value, ct);
        if (job is null)
            return Results.NotFound();

        job.Description = request.Description;
        job.CustomerId = request.CustomerId;
        job.Address = request.Address;
        job.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ChangeJobStatus(
        Guid id,
        ChangeStatusRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantProvider.TenantId.Value, ct);
        if (job is null)
            return Results.NotFound();

        var status = request.Status switch
        {
            "Aktiv" => JobStatus.Active,
            "Venter på deler" => JobStatus.WaitingForParts,
            "Fullført" => JobStatus.Completed,
            _ => (JobStatus?)null
        };

        if (status is null)
            return Results.BadRequest(new { error = $"Ugyldig status: {request.Status}" });

        job.Status = status.Value;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AddPartsItem(
        Guid id,
        AddPartsItemRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest(new { error = "Beskrivelse er påkrevd." });

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId))
            return Results.Unauthorized();

        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantProvider.TenantId.Value, ct);
        if (job is null)
            return Results.NotFound();

        var item = new JobPartsItem
        {
            JobId = id,
            Description = request.Description,
            Notes = request.Notes,
            Status = PartsItemStatus.Trengs,
            AddedById = personId
        };

        db.JobPartsItems.Add(item);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/jobs/{id}/parts/{item.Id}", new { id = item.Id });
    }

    private static async Task<IResult> UpdatePartsItem(
        Guid id,
        Guid partId,
        UpdatePartsItemRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var parentJob = await db.Jobs.FirstOrDefaultAsync(j => j.Id == id && j.TenantId == tenantProvider.TenantId.Value, ct);
        if (parentJob is null)
            return Results.NotFound();

        var item = await db.JobPartsItems.FirstOrDefaultAsync(pi => pi.Id == partId && pi.JobId == id, ct);
        if (item is null)
            return Results.NotFound();

        var status = request.Status switch
        {
            "Trengs" => PartsItemStatus.Trengs,
            "Bestilt" => PartsItemStatus.Bestilt,
            "Mottatt" => PartsItemStatus.Mottatt,
            _ => (PartsItemStatus?)null
        };

        if (status is null)
            return Results.BadRequest(new { error = $"Ugyldig status: {request.Status}" });

        item.Status = status.Value;
        item.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
