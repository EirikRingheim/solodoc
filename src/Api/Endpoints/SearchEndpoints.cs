using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Search;

namespace Solodoc.Api.Endpoints;

public static class SearchEndpoints
{
    public static WebApplication MapSearchEndpoints(this WebApplication app)
    {
        app.MapGet("/api/search", GlobalSearch).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GlobalSearch(
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct,
        string? q = null)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Results.Ok(new SearchResponse([], 0));

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;
        var term = q.ToLowerInvariant();
        var results = new List<SearchResultDto>();

        // Projects (max 5)
        var projects = await db.Projects
            .Where(p => p.TenantId == tenantId)
            .Where(p => p.Name.ToLower().Contains(term) ||
                        (p.ClientName != null && p.ClientName.ToLower().Contains(term)) ||
                        (p.Address != null && p.Address.ToLower().Contains(term)))
            .OrderByDescending(p => p.Name.ToLower().StartsWith(term))
            .ThenBy(p => p.Name)
            .Take(5)
            .Select(p => new SearchResultDto(
                "project",
                p.Id,
                p.Name,
                p.ClientName,
                p.Address))
            .ToListAsync(ct);
        results.AddRange(projects);

        // Jobs (max 5)
        var jobs = await db.Jobs
            .Where(j => j.TenantId == tenantId)
            .Where(j => j.Description.ToLower().Contains(term) ||
                        (j.Address != null && j.Address.ToLower().Contains(term)))
            .OrderByDescending(j => j.Description.ToLower().StartsWith(term))
            .ThenBy(j => j.Description)
            .Take(5)
            .Select(j => new SearchResultDto(
                "job",
                j.Id,
                j.Description,
                j.Address,
                null))
            .ToListAsync(ct);
        results.AddRange(jobs);

        // Deviations (max 5)
        var deviations = await db.Deviations
            .Where(d => d.TenantId == tenantId)
            .Where(d => d.Title.ToLower().Contains(term) ||
                        (d.Description != null && d.Description.ToLower().Contains(term)))
            .OrderByDescending(d => d.Title.ToLower().StartsWith(term))
            .ThenBy(d => d.Title)
            .Take(5)
            .Select(d => new SearchResultDto(
                "deviation",
                d.Id,
                d.Title,
                d.Status == DeviationStatus.Open ? "Åpen"
                    : d.Status == DeviationStatus.InProgress ? "Under behandling"
                    : "Lukket",
                d.Description))
            .ToListAsync(ct);
        results.AddRange(deviations);

        // Contacts (max 5)
        var contacts = await db.Contacts
            .Where(c => c.TenantId == tenantId)
            .Where(c => c.Name.ToLower().Contains(term) ||
                        (c.Email != null && c.Email.ToLower().Contains(term)) ||
                        (c.Phone != null && c.Phone.Contains(term)))
            .OrderByDescending(c => c.Name.ToLower().StartsWith(term))
            .ThenBy(c => c.Name)
            .Take(5)
            .Select(c => new SearchResultDto(
                "contact",
                c.Id,
                c.Name,
                c.Type.ToString(),
                c.Email))
            .ToListAsync(ct);
        results.AddRange(contacts);

        // Equipment (max 5)
        var equipment = await db.Equipment
            .Where(e => e.TenantId == tenantId)
            .Where(e => e.Name.ToLower().Contains(term) ||
                        (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(term)))
            .OrderByDescending(e => e.Name.ToLower().StartsWith(term))
            .ThenBy(e => e.Name)
            .Take(5)
            .Select(e => new SearchResultDto(
                "equipment",
                e.Id,
                e.Name,
                e.SerialNumber,
                null))
            .ToListAsync(ct);
        results.AddRange(equipment);

        // Chemicals (max 5)
        var chemicals = await db.Chemicals
            .Where(c => c.TenantId == tenantId)
            .Where(c => c.Name.ToLower().Contains(term) ||
                        (c.Manufacturer != null && c.Manufacturer.ToLower().Contains(term)))
            .OrderByDescending(c => c.Name.ToLower().StartsWith(term))
            .ThenBy(c => c.Name)
            .Take(5)
            .Select(c => new SearchResultDto(
                "chemical",
                c.Id,
                c.Name,
                c.Manufacturer,
                c.ProductNumber))
            .ToListAsync(ct);
        results.AddRange(chemicals);

        return Results.Ok(new SearchResponse(results, results.Count));
    }
}
