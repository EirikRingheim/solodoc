using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Auth;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.CheckIn;

namespace Solodoc.Api.Endpoints;

public static class CheckInEndpoints
{
    public static WebApplication MapCheckInEndpoints(this WebApplication app)
    {
        app.MapPost("/api/checkin", DoCheckIn).RequireAuthorization();
        app.MapPost("/api/checkout", DoCheckOut).RequireAuthorization();
        app.MapGet("/api/checkin/status", GetMyStatus).RequireAuthorization();
        app.MapGet("/api/checkin/on-site/{siteType}/{siteId:guid}", GetOnSite).RequireAuthorization();
        app.MapGet("/api/checkin/all-sites", GetAllSites).RequireAuthorization();
        app.MapGet("/api/checkin/history/{siteType}/{siteId:guid}", GetHistory).RequireAuthorization();
        app.MapGet("/api/checkin/log", GetMyLog).RequireAuthorization();
        app.MapGet("/api/checkin/qr/{slug}", QrLanding).AllowAnonymous();
        app.MapPost("/api/checkin/guest", GuestCheckIn).AllowAnonymous();
        app.MapGet("/api/checkin/qr-branding/{siteType}/{siteId:guid}", GetQrBranding).RequireAuthorization();
        app.MapPost("/api/checkin/generate-qr/{siteType}/{siteId:guid}", GenerateQrSlug).RequireAuthorization();
        app.MapGet("/api/checkin/export/{siteType}/{siteId:guid}", ExportCheckInLog).RequireAuthorization();

        return app;
    }

    private static (Guid? personId, bool valid) GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var pid) ? (pid, true) : (null, false);
    }

    // ─── Check In ───────────────────────────────────────────────────

    private static async Task<IResult> DoCheckIn(
        CheckInRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tp.TenantId is null) return Results.Unauthorized();

        // Check if already checked in somewhere
        var existing = await db.WorksiteCheckIns
            .FirstOrDefaultAsync(c => c.PersonId == personId!.Value
                && c.TenantId == tp.TenantId.Value
                && c.CheckedOutAt == null, ct);

        if (existing is not null)
        {
            // Auto-checkout from previous site
            existing.CheckedOutAt = DateTimeOffset.UtcNow;
            existing.LatitudeOut = request.Latitude;
            existing.LongitudeOut = request.Longitude;
        }

        var source = request.Source?.ToLowerInvariant() switch
        {
            "qrcode" or "qr" => "QrCode",
            "timeclock" or "timer" => "TimeClock",
            _ => "Manual"
        };

        var checkIn = new WorksiteCheckIn
        {
            TenantId = tp.TenantId.Value,
            PersonId = personId!.Value,
            ProjectId = request.ProjectId,
            JobId = request.JobId,
            LocationId = request.LocationId,
            CheckedInAt = DateTimeOffset.UtcNow,
            Source = source,
            Latitude = request.Latitude,
            Longitude = request.Longitude
        };

        db.WorksiteCheckIns.Add(checkIn);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { id = checkIn.Id });
    }

    // ─── Check Out ──────────────────────────────────────────────────

    private static async Task<IResult> DoCheckOut(
        CheckOutRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tp.TenantId is null) return Results.Unauthorized();

        var checkIn = await db.WorksiteCheckIns
            .FirstOrDefaultAsync(c => c.PersonId == personId!.Value
                && c.TenantId == tp.TenantId.Value
                && c.CheckedOutAt == null, ct);

        if (checkIn is null)
            return Results.BadRequest(new { error = "Du er ikke sjekket inn." });

        checkIn.CheckedOutAt = DateTimeOffset.UtcNow;
        checkIn.LatitudeOut = request.Latitude;
        checkIn.LongitudeOut = request.Longitude;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ─── My Status ──────────────────────────────────────────────────

    private static async Task<IResult> GetMyStatus(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tp.TenantId is null) return Results.Unauthorized();

        var active = await db.WorksiteCheckIns
            .FirstOrDefaultAsync(c => c.PersonId == personId!.Value
                && c.TenantId == tp.TenantId.Value
                && c.CheckedOutAt == null, ct);

        if (active is null)
            return Results.Ok(new MyCheckInStatusDto(null, false, null, null, null));

        var (siteName, siteType) = await ResolveSiteName(active, db, ct);

        return Results.Ok(new MyCheckInStatusDto(active.Id, true, siteName, siteType, active.CheckedInAt));
    }

    // ─── Who's On Site ──────────────────────────────────────────────

    private static async Task<IResult> GetOnSite(
        string siteType,
        Guid siteId,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var query = db.WorksiteCheckIns
            .Where(c => c.TenantId == tp.TenantId.Value && c.CheckedOutAt == null);

        query = siteType.ToLowerInvariant() switch
        {
            "project" => query.Where(c => c.ProjectId == siteId),
            "job" => query.Where(c => c.JobId == siteId),
            "location" => query.Where(c => c.LocationId == siteId),
            _ => query.Where(c => false)
        };

        var checkIns = await query
            .Select(c => new { c.PersonId, c.CheckedInAt, c.Source, c.IsGuest, c.GuestName, c.GuestCompany })
            .ToListAsync(ct);

        var personIds = checkIns.Where(c => c.PersonId.HasValue).Select(c => c.PersonId!.Value).Distinct().ToList();
        var persons = personIds.Count > 0
            ? await db.Persons.Where(p => personIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p, ct)
            : new Dictionary<Guid, Person>();
        var memberships = personIds.Count > 0
            ? await db.TenantMemberships.Where(m => personIds.Contains(m.PersonId) && m.TenantId == tp.TenantId.Value).ToDictionaryAsync(m => m.PersonId, m => m, ct)
            : new Dictionary<Guid, TenantMembership>();

        var result = checkIns.Select(c =>
        {
            if (c.IsGuest)
                return new OnSitePersonDto(Guid.Empty, c.GuestName ?? "Gjest", "Gjest", c.GuestCompany, c.CheckedInAt, c.Source);

            var pid = c.PersonId!.Value;
            persons.TryGetValue(pid, out var person);
            memberships.TryGetValue(pid, out var membership);
            var role = membership?.Role switch
            {
                TenantRole.TenantAdmin => "Admin",
                TenantRole.ProjectLeader => "Prosjektleder",
                _ => "Feltarbeider"
            };
            return new OnSitePersonDto(pid, person?.FullName ?? "", role, null, c.CheckedInAt, c.Source.ToString());
        }).OrderBy(p => p.CheckInAt).ToList();

        return Results.Ok(result);
    }

    // ─── All Sites Overview (Dashboard) ─────────────────────────────

    private static async Task<IResult> GetAllSites(
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var activeCheckIns = await db.WorksiteCheckIns
            .Where(c => c.TenantId == tp.TenantId.Value && c.CheckedOutAt == null)
            .Select(c => new { c.PersonId, c.ProjectId, c.JobId, c.LocationId, c.CheckedInAt, c.Source, c.IsGuest, c.GuestName, c.GuestCompany })
            .ToListAsync(ct);

        if (activeCheckIns.Count == 0)
            return Results.Ok(new List<SiteOverviewDto>());

        // Resolve registered person names
        var personIds = activeCheckIns.Where(c => c.PersonId.HasValue).Select(c => c.PersonId!.Value).Distinct().ToList();
        var persons = personIds.Count > 0
            ? await db.Persons.Where(p => personIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct)
            : new Dictionary<Guid, string>();

        var memberships = personIds.Count > 0
            ? await db.TenantMemberships.Where(m => personIds.Contains(m.PersonId) && m.TenantId == tp.TenantId.Value).ToDictionaryAsync(m => m.PersonId, m => m.Role, ct)
            : new Dictionary<Guid, TenantRole>();

        // Resolve site names
        var projectIds = activeCheckIns.Where(c => c.ProjectId.HasValue).Select(c => c.ProjectId!.Value).Distinct().ToList();
        var jobIds = activeCheckIns.Where(c => c.JobId.HasValue).Select(c => c.JobId!.Value).Distinct().ToList();
        var locationIds = activeCheckIns.Where(c => c.LocationId.HasValue).Select(c => c.LocationId!.Value).Distinct().ToList();

        var projectNames = projectIds.Count > 0
            ? await db.Projects.Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();
        var jobDescs = jobIds.Count > 0
            ? await db.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.Description ?? "", ct)
            : new Dictionary<Guid, string>();
        var locationNames = locationIds.Count > 0
            ? await db.Locations.Where(l => locationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Name, ct)
            : new Dictionary<Guid, string>();

        // Group by site
        var sites = new List<SiteOverviewDto>();

        OnSitePersonDto MapPerson(Guid? personId, bool isGuest, string? guestName, string? guestCompany, DateTimeOffset checkedInAt, string source)
        {
            if (isGuest)
                return new OnSitePersonDto(Guid.Empty, guestName ?? "Gjest", "Gjest", guestCompany, checkedInAt, source);
            if (!personId.HasValue)
                return new OnSitePersonDto(Guid.Empty, "Ukjent", "Ukjent", null, checkedInAt, source);
            persons.TryGetValue(personId.Value, out var pName);
            memberships.TryGetValue(personId.Value, out var role);
            var roleStr = role switch { TenantRole.TenantAdmin => "Admin", TenantRole.ProjectLeader => "Prosjektleder", _ => "Feltarbeider" };
            return new OnSitePersonDto(personId.Value, pName ?? "", roleStr, null, checkedInAt, source);
        }

        foreach (var g in activeCheckIns.Where(c => c.ProjectId.HasValue).GroupBy(c => c.ProjectId!.Value))
        {
            projectNames.TryGetValue(g.Key, out var n);
            var pList = g.Select(c => MapPerson(c.PersonId, c.IsGuest, c.GuestName, c.GuestCompany, c.CheckedInAt, c.Source)).OrderBy(p => p.CheckInAt).ToList();
            sites.Add(new SiteOverviewDto(g.Key, n ?? "Prosjekt", "Prosjekt", pList.Count, pList));
        }
        foreach (var g in activeCheckIns.Where(c => c.JobId.HasValue && !c.ProjectId.HasValue).GroupBy(c => c.JobId!.Value))
        {
            jobDescs.TryGetValue(g.Key, out var n);
            var pList = g.Select(c => MapPerson(c.PersonId, c.IsGuest, c.GuestName, c.GuestCompany, c.CheckedInAt, c.Source)).OrderBy(p => p.CheckInAt).ToList();
            sites.Add(new SiteOverviewDto(g.Key, n ?? "Oppdrag", "Oppdrag", pList.Count, pList));
        }
        foreach (var g in activeCheckIns.Where(c => c.LocationId.HasValue && !c.ProjectId.HasValue && !c.JobId.HasValue).GroupBy(c => c.LocationId!.Value))
        {
            locationNames.TryGetValue(g.Key, out var n);
            var pList = g.Select(c => MapPerson(c.PersonId, c.IsGuest, c.GuestName, c.GuestCompany, c.CheckedInAt, c.Source)).OrderBy(p => p.CheckInAt).ToList();
            sites.Add(new SiteOverviewDto(g.Key, n ?? "Lokasjon", "Lokasjon", pList.Count, pList));
        }

        return Results.Ok(sites.OrderByDescending(s => s.PersonCount).ToList());
    }

    // ─── History ────────────────────────────────────────────────────

    private static async Task<IResult> GetHistory(
        string siteType,
        Guid siteId,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct,
        int days = 30)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var since = DateTimeOffset.UtcNow.AddDays(-days);

        var query = db.WorksiteCheckIns
            .Where(c => c.TenantId == tp.TenantId.Value && c.CheckedInAt >= since);

        query = siteType.ToLowerInvariant() switch
        {
            "project" => query.Where(c => c.ProjectId == siteId),
            "job" => query.Where(c => c.JobId == siteId),
            "location" => query.Where(c => c.LocationId == siteId),
            _ => query.Where(c => false)
        };

        var checkIns = await query
            .OrderByDescending(c => c.CheckedInAt)
            .Select(c => new { c.Id, c.PersonId, c.CheckedInAt, c.CheckedOutAt, c.Source, c.AutoCheckedOut, c.Latitude, c.Longitude, c.IsGuest, c.GuestName })
            .Take(200)
            .ToListAsync(ct);

        var personIds = checkIns.Where(c => c.PersonId.HasValue).Select(c => c.PersonId!.Value).Distinct().ToList();
        var persons = personIds.Count > 0
            ? await db.Persons.Where(p => personIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct)
            : new Dictionary<Guid, string>();

        var result = checkIns.Select(c => new CheckInHistoryDto(
            c.Id,
            c.IsGuest ? (c.GuestName ?? "Gjest") : (c.PersonId.HasValue && persons.TryGetValue(c.PersonId.Value, out var name) ? name : ""),
            null,
            c.CheckedInAt,
            c.CheckedOutAt,
            c.Source.ToString(),
            c.AutoCheckedOut,
            c.Latitude,
            c.Longitude
        )).ToList();

        return Results.Ok(result);
    }

    // ─── My Check-in Log ──────────────────────────────────────────

    private static async Task<IResult> GetMyLog(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct,
        int days = 30)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tp.TenantId is null) return Results.Unauthorized();

        var since = DateTimeOffset.UtcNow.AddDays(-days);

        var checkIns = await db.WorksiteCheckIns
            .Where(c => c.TenantId == tp.TenantId.Value && c.PersonId == personId!.Value && c.CheckedInAt >= since)
            .OrderByDescending(c => c.CheckedInAt)
            .Select(c => new { c.Id, c.ProjectId, c.JobId, c.LocationId, c.CheckedInAt, c.CheckedOutAt, c.Source })
            .Take(100)
            .ToListAsync(ct);

        // Batch resolve site names
        var projectIds = checkIns.Where(c => c.ProjectId.HasValue).Select(c => c.ProjectId!.Value).Distinct().ToList();
        var jobIds = checkIns.Where(c => c.JobId.HasValue).Select(c => c.JobId!.Value).Distinct().ToList();
        var locationIds = checkIns.Where(c => c.LocationId.HasValue).Select(c => c.LocationId!.Value).Distinct().ToList();

        var projectNames = projectIds.Count > 0
            ? await db.Projects.Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();
        var jobDescs = jobIds.Count > 0
            ? await db.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.Description ?? "", ct)
            : new Dictionary<Guid, string>();
        var locationNames = locationIds.Count > 0
            ? await db.Locations.Where(l => locationIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Name, ct)
            : new Dictionary<Guid, string>();

        var result = checkIns.Select(c =>
        {
            string siteName, siteType;
            if (c.ProjectId.HasValue)
            {
                projectNames.TryGetValue(c.ProjectId.Value, out var n);
                siteName = n ?? "Prosjekt";
                siteType = "Prosjekt";
            }
            else if (c.JobId.HasValue)
            {
                jobDescs.TryGetValue(c.JobId.Value, out var n);
                siteName = n ?? "Oppdrag";
                siteType = "Oppdrag";
            }
            else if (c.LocationId.HasValue)
            {
                locationNames.TryGetValue(c.LocationId.Value, out var n);
                siteName = n ?? "Lokasjon";
                siteType = "Lokasjon";
            }
            else
            {
                siteName = "Ukjent";
                siteType = "Ukjent";
            }

            var duration = c.CheckedOutAt.HasValue
                ? (c.CheckedOutAt.Value - c.CheckedInAt).TotalMinutes
                : (DateTimeOffset.UtcNow - c.CheckedInAt).TotalMinutes;

            return new CheckInLogEntryDto(c.Id, siteName, siteType, c.CheckedInAt, c.CheckedOutAt, c.Source, (int)duration);
        }).ToList();

        return Results.Ok(result);
    }

    // ─── Guest Check-in (anonymous) ────────────────────────────────

    private static async Task<IResult> GuestCheckIn(
        GuestCheckInRequest request,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        // Find the site by slug
        Guid? projectId = null, jobId = null, locationId = null;
        Guid tenantId;

        var project = await db.Projects
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.QrCodeSlug == request.Slug && !p.IsDeleted, ct);

        if (project is not null)
        {
            projectId = project.Id;
            tenantId = project.TenantId;
        }
        else
        {
            var location = await db.Locations
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.QrCodeSlug == request.Slug && !l.IsDeleted, ct);

            if (location is not null)
            {
                locationId = location.Id;
                tenantId = location.TenantId;
            }
            else
            {
                return Results.NotFound(new { error = "QR-kode ikke funnet." });
            }
        }

        var checkIn = new WorksiteCheckIn
        {
            TenantId = tenantId,
            PersonId = null,
            ProjectId = projectId,
            JobId = jobId,
            LocationId = locationId,
            CheckedInAt = DateTimeOffset.UtcNow,
            Source = "QrCode",
            IsGuest = true,
            GuestName = request.Name.Trim(),
            GuestCompany = request.Company?.Trim(),
            GuestPhone = request.Phone?.Trim()
        };

        db.WorksiteCheckIns.Add(checkIn);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { id = checkIn.Id, name = checkIn.GuestName });
    }

    // ─── QR Landing ─────────────────────────────────────────────────

    private static async Task<IResult> QrLanding(
        string slug,
        SolodocDbContext db,
        IFileStorageService fileStorage,
        CancellationToken ct)
    {
        // Find the project or location with this slug
        var project = await db.Projects
            .FirstOrDefaultAsync(p => p.QrCodeSlug == slug, ct);

        if (project is not null)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == project.TenantId, ct);
            var logoUrl = await GetTenantLogoUrl(tenant, fileStorage, ct);
            return Results.Ok(new QrCodeInfoDto(slug, $"/checkin/qr/{slug}", project.Name, "Prosjekt",
                project.Id, tenant?.Name, tenant?.AccentColor, logoUrl));
        }

        var location = await db.Locations
            .FirstOrDefaultAsync(l => l.QrCodeSlug == slug, ct);

        if (location is not null)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == location.TenantId, ct);
            var logoUrl = await GetTenantLogoUrl(tenant, fileStorage, ct);
            return Results.Ok(new QrCodeInfoDto(slug, $"/checkin/qr/{slug}", location.Name, "Lokasjon",
                location.Id, tenant?.Name, tenant?.AccentColor, logoUrl));
        }

        return Results.NotFound(new { error = "QR-kode ikke funnet." });
    }

    // ─── QR Branding (for printable card) ───────────────────────────

    private static async Task<IResult> GetQrBranding(
        string siteType,
        Guid siteId,
        SolodocDbContext db,
        ITenantProvider tp,
        IFileStorageService fileStorage,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

        string? siteName = null;
        string? slug = null;

        if (siteType.ToLowerInvariant() == "project")
        {
            var p = await db.Projects.FirstOrDefaultAsync(x => x.Id == siteId && x.TenantId == tp.TenantId.Value, ct);
            siteName = p?.Name;
            slug = p?.QrCodeSlug;
        }
        else if (siteType.ToLowerInvariant() == "location")
        {
            var l = await db.Locations.FirstOrDefaultAsync(x => x.Id == siteId && x.TenantId == tp.TenantId.Value, ct);
            siteName = l?.Name;
            slug = l?.QrCodeSlug;
        }

        if (siteName is null) return Results.NotFound();

        var logoUrl = await GetTenantLogoUrl(tenant, fileStorage, ct);

        return Results.Ok(new QrCodeInfoDto(
            slug ?? "",
            slug is not null ? $"/checkin/qr/{slug}" : "",
            siteName,
            siteType,
            siteId,
            tenant.Name,
            tenant.AccentColor,
            logoUrl));
    }

    private static async Task<string?> GetTenantLogoUrl(
        Domain.Entities.Auth.Tenant? tenant,
        IFileStorageService fileStorage,
        CancellationToken ct)
    {
        if (tenant?.LogoFileKey is null) return null;
        try
        {
            return await fileStorage.GetPresignedUrlAsync(tenant.LogoFileKey, TimeSpan.FromHours(1), ct);
        }
        catch
        {
            return null;
        }
    }

    // ─── Generate QR Slug ───────────────────────────────────────────

    private static async Task<IResult> GenerateQrSlug(
        string siteType,
        Guid siteId,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var slug = Guid.NewGuid().ToString("N")[..8]; // Short unique slug

        if (siteType.ToLowerInvariant() == "project")
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == siteId && p.TenantId == tp.TenantId.Value, ct);
            if (project is null) return Results.NotFound();
            project.QrCodeSlug = slug;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { slug, url = $"/checkin/qr/{slug}" });
        }
        else if (siteType.ToLowerInvariant() == "location")
        {
            var location = await db.Locations.FirstOrDefaultAsync(l => l.Id == siteId && l.TenantId == tp.TenantId.Value, ct);
            if (location is null) return Results.NotFound();
            location.QrCodeSlug = slug;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { slug, url = $"/checkin/qr/{slug}" });
        }

        return Results.BadRequest(new { error = "Ugyldig siteType." });
    }

    // ─── Export CSV ─────────────────────────────────────────────────

    private static async Task<IResult> ExportCheckInLog(
        string siteType,
        Guid siteId,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct,
        int days = 90)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var since = DateTimeOffset.UtcNow.AddDays(-days);
        var query = db.WorksiteCheckIns
            .Where(c => c.TenantId == tp.TenantId.Value && c.CheckedInAt >= since);

        query = siteType.ToLowerInvariant() switch
        {
            "project" => query.Where(c => c.ProjectId == siteId),
            "job" => query.Where(c => c.JobId == siteId),
            "location" => query.Where(c => c.LocationId == siteId),
            _ => query.Where(c => false)
        };

        var checkIns = await query
            .OrderByDescending(c => c.CheckedInAt)
            .Select(c => new { c.PersonId, c.CheckedInAt, c.CheckedOutAt, c.Source, c.IsGuest, c.GuestName, c.GuestCompany })
            .Take(5000)
            .ToListAsync(ct);

        var personIds = checkIns.Where(c => c.PersonId.HasValue).Select(c => c.PersonId!.Value).Distinct().ToList();
        var persons = personIds.Count > 0
            ? await db.Persons.Where(p => personIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct)
            : new Dictionary<Guid, string>();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Navn,Firma,Innsjekket,Utsjekket,Varighet (min),Kilde");

        foreach (var c in checkIns)
        {
            var name = c.IsGuest ? (c.GuestName ?? "Gjest") : (c.PersonId.HasValue && persons.TryGetValue(c.PersonId.Value, out var n) ? n : "Ukjent");
            var company = c.IsGuest ? (c.GuestCompany ?? "") : "";
            var checkedIn = c.CheckedInAt.ToOffset(TimeSpan.FromHours(2)).ToString("dd.MM.yyyy HH:mm");
            var checkedOut = c.CheckedOutAt?.ToOffset(TimeSpan.FromHours(2)).ToString("dd.MM.yyyy HH:mm") ?? "";
            var duration = c.CheckedOutAt.HasValue ? ((int)(c.CheckedOutAt.Value - c.CheckedInAt).TotalMinutes).ToString() : "";
            csv.AppendLine($"\"{name}\",\"{company}\",{checkedIn},{checkedOut},{duration},{c.Source}");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return Results.File(bytes, "text/csv; charset=utf-8", $"innsjekking-{siteType}-{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}.csv");
    }

    // ─── Helpers ────────────────────────────────────────────────────

    private static async Task<(string name, string type)> ResolveSiteName(
        WorksiteCheckIn checkIn, SolodocDbContext db, CancellationToken ct)
    {
        if (checkIn.ProjectId.HasValue)
        {
            var name = await db.Projects.Where(p => p.Id == checkIn.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(ct);
            return (name ?? "Prosjekt", "Prosjekt");
        }
        if (checkIn.JobId.HasValue)
        {
            var name = await db.Jobs.Where(j => j.Id == checkIn.JobId).Select(j => j.Description).FirstOrDefaultAsync(ct);
            return (name ?? "Oppdrag", "Oppdrag");
        }
        if (checkIn.LocationId.HasValue)
        {
            var name = await db.Locations.Where(l => l.Id == checkIn.LocationId).Select(l => l.Name).FirstOrDefaultAsync(ct);
            return (name ?? "Lokasjon", "Lokasjon");
        }
        return ("Ukjent", "Ukjent");
    }
}
