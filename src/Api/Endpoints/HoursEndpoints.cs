using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Hours;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Common;
using Solodoc.Shared.Hours;

namespace Solodoc.Api.Endpoints;

public static class HoursEndpoints
{
    public static WebApplication MapHoursEndpoints(this WebApplication app)
    {
        // Time entries (employee)
        app.MapGet("/api/hours", ListTimeEntries).RequireAuthorization();
        app.MapGet("/api/hours/active-clock", GetActiveClock).RequireAuthorization();
        app.MapPost("/api/hours/clock-in", ClockIn).RequireAuthorization();
        app.MapPost("/api/hours/clock-out", ClockOut).RequireAuthorization();
        app.MapPost("/api/hours", CreateManualTimeEntry).RequireAuthorization();
        app.MapPut("/api/hours/{id:guid}", UpdateTimeEntry).RequireAuthorization();
        app.MapDelete("/api/hours/{id:guid}", DeleteTimeEntry).RequireAuthorization();

        // Approval flow
        app.MapPatch("/api/hours/{id:guid}/submit", SubmitTimeEntry).RequireAuthorization();
        app.MapPatch("/api/hours/{id:guid}/approve", ApproveTimeEntry).RequireAuthorization();
        app.MapPatch("/api/hours/{id:guid}/reject", RejectTimeEntry).RequireAuthorization();

        // Admin view
        app.MapGet("/api/hours/admin", ListAllTimeEntries).RequireAuthorization();
        app.MapGet("/api/hours/admin/day-detail", GetDayDetail).RequireAuthorization();
        app.MapPost("/api/hours/admin/approve-day", ApproveDayEntries).RequireAuthorization();

        // My schedule
        app.MapGet("/api/hours/my-schedule", GetMySchedule).RequireAuthorization();

        // Overtime bank
        app.MapPost("/api/hours/overtime-bank/credit", CreditOvertimeBank).RequireAuthorization();

        // Operation-based allowances on time entries
        app.MapPost("/api/hours/{id:guid}/allowances", AddTimeEntryAllowance).RequireAuthorization();

        // My heatmap (employee view)
        app.MapGet("/api/hours/my-heatmap", GetMyHeatmap).RequireAuthorization();

        // Heatmap
        app.MapGet("/api/hours/heatmap", GetHoursHeatmap).RequireAuthorization();

        // Schedules
        app.MapGet("/api/schedules", ListSchedules).RequireAuthorization();
        app.MapPost("/api/schedules", CreateSchedule).RequireAuthorization();

        // Allowance rules
        app.MapGet("/api/allowances/rules", ListAllowanceRules).RequireAuthorization();
        app.MapPost("/api/allowances/rules", CreateAllowanceRule).RequireAuthorization();
        app.MapPut("/api/allowances/rules/{id:guid}", UpdateAllowanceRule).RequireAuthorization();
        app.MapDelete("/api/allowances/rules/{id:guid}", DeleteAllowanceRule).RequireAuthorization();

        return app;
    }

    private static (Guid? personId, bool valid) GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue("sub");
        if (Guid.TryParse(claim, out var personId))
            return (personId, true);
        return (null, false);
    }

    private static async Task<bool> IsAdminOrProjectLeader(Guid personId, Guid tenantId, SolodocDbContext db, CancellationToken ct)
    {
        var m = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == personId && m.TenantId == tenantId && m.State == TenantMembershipState.Active, ct);
        return m?.Role is TenantRole.TenantAdmin or TenantRole.ProjectLeader;
    }

    private static TimeEntryCategory ParseCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return TimeEntryCategory.Arbeid;

        return category.ToLowerInvariant() switch
        {
            "arbeid" => TimeEntryCategory.Arbeid,
            "reise" => TimeEntryCategory.Reise,
            "kontorarbeid" => TimeEntryCategory.Kontorarbeid,
            "lagerarbeid" => TimeEntryCategory.Lagerarbeid,
            "kurs" => TimeEntryCategory.Kurs,
            "annet" => TimeEntryCategory.Annet,
            _ => TimeEntryCategory.Arbeid
        };
    }

    private static string CategoryToString(TimeEntryCategory cat) => cat switch
    {
        TimeEntryCategory.Arbeid => "Arbeid",
        TimeEntryCategory.Reise => "Reise",
        TimeEntryCategory.Kontorarbeid => "Kontorarbeid",
        TimeEntryCategory.Lagerarbeid => "Lagerarbeid",
        TimeEntryCategory.Kurs => "Kurs",
        TimeEntryCategory.Annet => "Annet",
        _ => "Arbeid"
    };

    private static string AbsenceTypeToShort(AbsenceType t) => t switch
    {
        AbsenceType.Ferie => "Ferie",
        AbsenceType.Sykmelding or AbsenceType.Egenmelding => "Syk",
        AbsenceType.Lege => "Lege",
        AbsenceType.Tannlege => "Tannlege",
        AbsenceType.Foreldrepermisjon => "Permisjon",
        AbsenceType.Permisjon => "Permisjon",
        AbsenceType.Avspasering => "Avspass.",
        _ => "Fravær"
    };

    private static string StatusToString(TimeEntryStatus status) => status switch
    {
        TimeEntryStatus.Draft => "Utkast",
        TimeEntryStatus.Submitted => "Innsendt",
        TimeEntryStatus.Approved => "Godkjent",
        TimeEntryStatus.Rejected => "Avvist",
        _ => "Utkast"
    };

    // ─── Active Clock ────────────────────────────────────────────────

    private static async Task<IResult> GetActiveClock(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        try
        {
            var entry = await db.TimeEntries
                .FirstOrDefaultAsync(t =>
                    t.PersonId == personId!.Value &&
                    t.ClockIn != null &&
                    t.ClockOut == null, ct);

            if (entry is null || entry.ClockIn is null)
                return Results.Ok((ActiveClockDto?)null);

            string? projectName = null;
            if (entry.ProjectId.HasValue)
                projectName = await db.Projects
                    .Where(p => p.Id == entry.ProjectId.Value)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync(ct);

            return Results.Ok(new ActiveClockDto(entry.Id, entry.ClockIn.Value, projectName, entry.ProjectId));
        }
        catch
        {
            return Results.Ok((ActiveClockDto?)null);
        }
    }

    // ─── Time Entries (Employee) ─────────────────────────────────────

    private static async Task<IResult> ListTimeEntries(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        string? weekOf = null,
        Guid? projectId = null,
        string? status = null)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var query = db.TimeEntries
            .Where(t => t.PersonId == personId!.Value);

        if (!string.IsNullOrWhiteSpace(weekOf) && DateOnly.TryParse(weekOf, out var weekDate))
        {
            // Calculate Monday of the given week
            var dayOffset = ((int)weekDate.DayOfWeek + 6) % 7; // Monday=0
            var monday = weekDate.AddDays(-dayOffset);
            var sunday = monday.AddDays(6);
            query = query.Where(t => t.Date >= monday && t.Date <= sunday);
        }

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsedStatus = status.ToLowerInvariant() switch
            {
                "utkast" or "draft" => (TimeEntryStatus?)TimeEntryStatus.Draft,
                "innsendt" or "submitted" => TimeEntryStatus.Submitted,
                "godkjent" or "approved" => TimeEntryStatus.Approved,
                "avvist" or "rejected" => TimeEntryStatus.Rejected,
                _ => null
            };
            if (parsedStatus.HasValue)
                query = query.Where(t => t.Status == parsedStatus.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var rawEntries = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id, t.Date, t.Hours, t.OvertimeHours, t.BreakMinutes,
                t.Category, t.Status, t.ProjectId, t.JobId,
                t.StartTime, t.EndTime, t.Notes, t.IsManual
            })
            .ToListAsync(ct);

        var entryIds = rawEntries.Select(e => e.Id).ToList();
        var projectIds = rawEntries.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).Distinct().ToList();
        var jobIds = rawEntries.Where(e => e.JobId.HasValue).Select(e => e.JobId!.Value).Distinct().ToList();

        var projectNames = projectIds.Count > 0
            ? await db.Projects.Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();
        var jobDescriptions = jobIds.Count > 0
            ? await db.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.Description ?? "", ct)
            : new Dictionary<Guid, string>();

        var allowances = entryIds.Count > 0
            ? await db.TimeEntryAllowances
                .Where(a => entryIds.Contains(a.TimeEntryId))
                .Join(db.AllowanceRules, a => a.AllowanceRuleId, r => r.Id,
                    (a, r) => new { a.TimeEntryId, r.Name, r.Type, a.Hours, a.Amount })
                .ToListAsync(ct)
            : [];

        var items = rawEntries.Select(t => new TimeEntryListItemDto(
            t.Id, t.Date, t.Hours, t.OvertimeHours, t.BreakMinutes,
            CategoryToString(t.Category),
            StatusToString(t.Status),
            null,
            t.ProjectId,
            t.ProjectId.HasValue && projectNames.TryGetValue(t.ProjectId.Value, out var pn) ? pn : null,
            t.JobId,
            t.JobId.HasValue && jobDescriptions.TryGetValue(t.JobId.Value, out var jd) ? jd : null,
            t.StartTime, t.EndTime, t.Notes, t.IsManual,
            allowances.Where(a => a.TimeEntryId == t.Id)
                .Select(a => new TimeEntryAllowanceTagDto(a.Name, a.Type.ToString(), a.Hours, a.Amount))
                .ToList()
        )).ToList();

        return Results.Ok(new PagedResult<TimeEntryListItemDto>(items, totalCount, page, pageSize));
    }

    private static async Task<IResult> ClockIn(
        ClockInRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        // Block subcontractors from time registration
        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == personId!.Value && m.TenantId == tenantProvider.TenantId.Value
                && m.State == TenantMembershipState.Active, ct);
        if (membership is null)
        {
            // Check if subcontractor — blocked unless HoursRegistrationEnabled
            var subAccess = await db.SubcontractorAccesses
                .FirstOrDefaultAsync(s => s.PersonId == personId!.Value && s.TenantId == tenantProvider.TenantId.Value
                    && s.State == SubcontractorAccessState.Active, ct);
            if (subAccess is not null && !subAccess.HoursRegistrationEnabled)
                return Results.BadRequest(new { error = "Underentreprenører kan ikke registrere timer. Admin kan aktivere dette i innstillinger." });
            return Results.Unauthorized();
        }

        // Check if there's already an active clock-in
        var activeEntry = await db.TimeEntries
            .FirstOrDefaultAsync(t =>
                t.PersonId == personId!.Value &&
                t.ClockIn != null &&
                t.ClockOut == null, ct);

        if (activeEntry is not null)
            return Results.BadRequest(new { error = "Du er allerede stemplet inn." });

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hasAbsenceToday = await db.Absences
            .AnyAsync(a => a.PersonId == personId!.Value
                && a.Status != AbsenceStatus.Rejected
                && a.StartDate <= today && a.EndDate >= today, ct);
        if (hasAbsenceToday)
            return Results.BadRequest(new { error = "Du har registrert fravær i dag. Slett fraværet først." });

        // Block clock-in on finished/cancelled projects
        if (request.ProjectId.HasValue)
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId.Value, ct);
            if (project is not null && project.Status is ProjectStatus.Completed or ProjectStatus.Cancelled)
                return Results.BadRequest(new { error = "Kan ikke stemple inn på et fullført eller kansellert prosjekt." });
        }

        var now = DateTimeOffset.UtcNow;
        var clockStart = request.StartTime ?? now;
        // Don't allow future start times
        if (clockStart > now) clockStart = now;
        var entry = new TimeEntry
        {
            TenantId = tenantProvider.TenantId.Value,
            PersonId = personId!.Value,
            Date = DateOnly.FromDateTime(clockStart.UtcDateTime),
            ClockIn = clockStart,
            StartTime = clockStart,
            Category = ParseCategory(request.Category),
            Status = TimeEntryStatus.Draft,
            ProjectId = request.ProjectId,
            JobId = request.JobId,
            GpsLatitudeIn = request.Latitude,
            GpsLongitudeIn = request.Longitude,
            IsManual = false
        };

        db.TimeEntries.Add(entry);

        // Auto check-in: when clocking in with a project/job, also create a worksite check-in
        if (request.ProjectId.HasValue || request.JobId.HasValue)
        {
            // Check out from any existing check-in first
            var existingCheckIn = await db.WorksiteCheckIns
                .FirstOrDefaultAsync(c => c.PersonId == personId!.Value
                    && c.TenantId == tenantProvider.TenantId.Value
                    && c.CheckedOutAt == null, ct);
            if (existingCheckIn is not null)
            {
                existingCheckIn.CheckedOutAt = now;
                existingCheckIn.LatitudeOut = request.Latitude;
                existingCheckIn.LongitudeOut = request.Longitude;
            }

            db.WorksiteCheckIns.Add(new Domain.Entities.Auth.WorksiteCheckIn
            {
                TenantId = tenantProvider.TenantId.Value,
                PersonId = personId!.Value,
                ProjectId = request.ProjectId,
                JobId = request.JobId,
                CheckedInAt = now,
                Source = "TimeClock",
                Latitude = request.Latitude,
                Longitude = request.Longitude
            });
        }

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hours/{entry.Id}", new { id = entry.Id });
    }

    private static async Task<IResult> ClockOut(
        ClockOutRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t =>
                t.PersonId == personId!.Value &&
                t.ClockIn != null &&
                t.ClockOut == null, ct);

        if (entry is null)
            return Results.NotFound(new { error = "Ingen aktiv innstempeling funnet." });

        var now = DateTimeOffset.UtcNow;
        entry.ClockOut = now;
        entry.EndTime = now;
        entry.GpsLatitudeOut = request.Latitude;
        entry.GpsLongitudeOut = request.Longitude;

        // Update break if provided
        if (request.BreakMinutes.HasValue)
            entry.BreakMinutes = request.BreakMinutes.Value;

        // Calculate hours: (ClockOut - ClockIn).TotalHours - BreakMinutes / 60
        var totalHours = (decimal)(now - entry.ClockIn!.Value).TotalHours;
        // Cap at 24 hours max per entry
        totalHours = Math.Min(totalHours, 24m);
        entry.Hours = Math.Max(0, totalHours - (decimal)entry.BreakMinutes / 60m);

        // Auto check-out: when clocking out, also check out from worksite
        var activeCheckIn = await db.WorksiteCheckIns
            .FirstOrDefaultAsync(c => c.PersonId == personId!.Value && c.CheckedOutAt == null, ct);
        if (activeCheckIn is not null)
        {
            activeCheckIn.CheckedOutAt = now;
            activeCheckIn.LatitudeOut = request.Latitude;
            activeCheckIn.LongitudeOut = request.Longitude;
        }

        await db.SaveChangesAsync(ct);

        var dto = new TimeEntryDetailDto(
            entry.Id,
            entry.Date,
            entry.Hours,
            entry.OvertimeHours,
            entry.BreakMinutes,
            CategoryToString(entry.Category),
            StatusToString(entry.Status),
            null, entry.ProjectId,
            null, entry.JobId,
            entry.StartTime,
            entry.EndTime,
            entry.Notes,
            entry.IsManual,
            []);

        return Results.Ok(dto);
    }

    private static async Task<IResult> CreateManualTimeEntry(
        ManualTimeEntryRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        // Block subcontractors from time registration
        var hasMembership = await db.TenantMemberships
            .AnyAsync(m => m.PersonId == personId!.Value && m.TenantId == tenantProvider.TenantId.Value
                && m.State == TenantMembershipState.Active, ct);
        if (!hasMembership)
        {
            var subAccess2 = await db.SubcontractorAccesses
                .FirstOrDefaultAsync(s => s.PersonId == personId!.Value && s.TenantId == tenantProvider.TenantId.Value
                    && s.State == SubcontractorAccessState.Active, ct);
            if (subAccess2 is not null && !subAccess2.HoursRegistrationEnabled)
                return Results.BadRequest(new { error = "Underentreprenører kan ikke registrere timer." });
            return Results.Unauthorized();
        }

        // Check for conflicting absence on this day
        var hasAbsence = await db.Absences
            .AnyAsync(a => a.PersonId == personId!.Value
                && a.Status != AbsenceStatus.Rejected
                && a.StartDate <= request.Date && a.EndDate >= request.Date, ct);
        if (hasAbsence)
            return Results.BadRequest(new { error = "Du har registrert fravær denne dagen. Slett fraværet forst." });

        // Calculate hours from start/end if provided, otherwise use Hours directly
        decimal hours;
        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            var breakMins = request.BreakMinutes ?? 0;
            hours = Math.Max(0, (decimal)(request.EndTime.Value - request.StartTime.Value).TotalHours
                - (decimal)breakMins / 60m);
        }
        else if (request.Hours.HasValue && request.Hours.Value > 0)
        {
            hours = request.Hours.Value;
        }
        else
        {
            return Results.BadRequest(new { error = "Oppgi timer eller start/sluttid." });
        }

        var entry = new TimeEntry
        {
            TenantId = tenantProvider.TenantId.Value,
            PersonId = personId!.Value,
            Date = request.Date,
            Hours = hours,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            BreakMinutes = request.BreakMinutes ?? 0,
            ProjectId = request.ProjectId,
            JobId = request.JobId,
            Category = ParseCategory(request.Category),
            Notes = request.Notes,
            Status = TimeEntryStatus.Draft,
            IsManual = true
        };

        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hours/{entry.Id}", new { id = entry.Id });
    }

    private static async Task<IResult> UpdateTimeEntry(
        Guid id,
        ManualTimeEntryRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id && t.PersonId == personId!.Value, ct);

        if (entry is null)
            return Results.NotFound();

        if (entry.Status != TimeEntryStatus.Draft)
            return Results.BadRequest(new { error = "Kun utkast kan redigeres." });

        entry.Date = request.Date;
        if (request.StartTime.HasValue && request.EndTime.HasValue)
        {
            entry.StartTime = request.StartTime;
            entry.EndTime = request.EndTime;
            entry.BreakMinutes = request.BreakMinutes ?? entry.BreakMinutes;
            entry.Hours = Math.Max(0, (decimal)(request.EndTime.Value - request.StartTime.Value).TotalHours
                - (decimal)entry.BreakMinutes / 60m);
        }
        else if (request.Hours.HasValue)
        {
            entry.Hours = request.Hours.Value;
        }
        entry.ProjectId = request.ProjectId;
        entry.JobId = request.JobId;
        entry.Category = ParseCategory(request.Category);
        entry.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteTimeEntry(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id && t.PersonId == personId!.Value, ct);

        if (entry is null)
            return Results.NotFound();

        if (entry.Status != TimeEntryStatus.Draft)
            return Results.BadRequest(new { error = "Kun utkast kan slettes." });

        // Remove any allowances first
        var allowances = await db.TimeEntryAllowances
            .Where(a => a.TimeEntryId == id)
            .ToListAsync(ct);
        db.TimeEntryAllowances.RemoveRange(allowances);

        db.TimeEntries.Remove(entry);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }

    // ─── Approval Flow ──────────────────────────────────────────────

    private static async Task<IResult> SubmitTimeEntry(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id && t.PersonId == personId!.Value, ct);

        if (entry is null)
            return Results.NotFound();

        if (entry.Status != TimeEntryStatus.Draft)
            return Results.BadRequest(new { error = "Kun utkast kan sendes inn." });

        entry.Status = TimeEntryStatus.Submitted;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { status = StatusToString(entry.Status) });
    }

    private static async Task<IResult> ApproveTimeEntry(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        // Admin or ProjectLeader check
        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.PersonId == personId!.Value &&
                m.State == TenantMembershipState.Active, ct);

        if (membership is null || (membership.Role != TenantRole.TenantAdmin && membership.Role != TenantRole.ProjectLeader))
            return Results.Forbid();

        var tenantId = membership.TenantId;
        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);

        if (entry is null)
            return Results.NotFound();

        if (entry.Status != TimeEntryStatus.Submitted)
            return Results.BadRequest(new { error = "Kun innsendte timer kan godkjennes." });

        entry.Status = TimeEntryStatus.Approved;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { status = StatusToString(entry.Status) });
    }

    private static async Task<IResult> RejectTimeEntry(
        Guid id,
        ApproveRejectRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null) return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == personId!.Value && m.State == TenantMembershipState.Active, ct);
        if (membership is null || (membership.Role != TenantRole.TenantAdmin && membership.Role != TenantRole.ProjectLeader))
            return Results.Forbid();

        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantProvider.TenantId.Value, ct);

        if (entry is null)
            return Results.NotFound();

        if (entry.Status != TimeEntryStatus.Submitted)
            return Results.BadRequest(new { error = "Kun innsendte timer kan avvises." });

        entry.Status = TimeEntryStatus.Rejected;
        entry.Notes = string.IsNullOrWhiteSpace(request.Reason)
            ? entry.Notes
            : $"{entry.Notes}\n[Avvist: {request.Reason}]".Trim();

        await db.SaveChangesAsync(ct);

        return Results.Ok(new { status = StatusToString(entry.Status) });
    }

    // ─── Admin View ─────────────────────────────────────────────────

    private static async Task<IResult> ListAllTimeEntries(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct,
        int page = 1,
        int pageSize = 20,
        Guid? personId = null,
        Guid? projectId = null,
        string? weekOf = null,
        string? status = null)
    {
        var (currentPersonId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();
        if (!await IsAdminOrProjectLeader(currentPersonId!.Value, tenantProvider.TenantId.Value, db, ct))
            return Results.Forbid();

        // Admin check via tenant membership role
        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.PersonId == currentPersonId!.Value &&
                m.State == TenantMembershipState.Active, ct);

        if (membership is null || membership.Role != TenantRole.TenantAdmin)
            return Results.Forbid();

        var query = db.TimeEntries
            .Where(t => t.TenantId == tenantProvider.TenantId.Value);

        if (personId.HasValue)
            query = query.Where(t => t.PersonId == personId.Value);

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(weekOf) && DateOnly.TryParse(weekOf, out var weekDate))
        {
            var dayOffset = ((int)weekDate.DayOfWeek + 6) % 7;
            var monday = weekDate.AddDays(-dayOffset);
            var sunday = monday.AddDays(6);
            query = query.Where(t => t.Date >= monday && t.Date <= sunday);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var parsedStatus = status.ToLowerInvariant() switch
            {
                "utkast" or "draft" => (TimeEntryStatus?)TimeEntryStatus.Draft,
                "innsendt" or "submitted" => TimeEntryStatus.Submitted,
                "godkjent" or "approved" => TimeEntryStatus.Approved,
                "avvist" or "rejected" => TimeEntryStatus.Rejected,
                _ => null
            };
            if (parsedStatus.HasValue)
                query = query.Where(t => t.Status == parsedStatus.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var rawEntries = await query
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id, t.PersonId, t.Date, t.Hours, t.OvertimeHours, t.BreakMinutes,
                t.Category, t.Status, t.ProjectId, t.JobId,
                t.StartTime, t.EndTime, t.Notes, t.IsManual
            })
            .ToListAsync(ct);

        var entryIds = rawEntries.Select(e => e.Id).ToList();
        var personIds = rawEntries.Select(e => e.PersonId).Distinct().ToList();
        var projectIds = rawEntries.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).Distinct().ToList();
        var jobIds = rawEntries.Where(e => e.JobId.HasValue).Select(e => e.JobId!.Value).Distinct().ToList();

        var personNames = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);
        var projectNames = projectIds.Count > 0
            ? await db.Projects.Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();
        var jobDescriptions = jobIds.Count > 0
            ? await db.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.Description ?? "", ct)
            : new Dictionary<Guid, string>();

        var allowances = entryIds.Count > 0
            ? await db.TimeEntryAllowances
                .Where(a => entryIds.Contains(a.TimeEntryId))
                .Join(db.AllowanceRules, a => a.AllowanceRuleId, r => r.Id,
                    (a, r) => new { a.TimeEntryId, r.Name, r.Type, a.Hours, a.Amount })
                .ToListAsync(ct)
            : [];

        var items = rawEntries.Select(t => new TimeEntryListItemDto(
            t.Id, t.Date, t.Hours, t.OvertimeHours, t.BreakMinutes,
            CategoryToString(t.Category),
            StatusToString(t.Status),
            personNames.TryGetValue(t.PersonId, out var en) ? en : null,
            t.ProjectId,
            t.ProjectId.HasValue && projectNames.TryGetValue(t.ProjectId.Value, out var pn) ? pn : null,
            t.JobId,
            t.JobId.HasValue && jobDescriptions.TryGetValue(t.JobId.Value, out var jd) ? jd : null,
            t.StartTime, t.EndTime, t.Notes, t.IsManual,
            allowances.Where(a => a.TimeEntryId == t.Id)
                .Select(a => new TimeEntryAllowanceTagDto(a.Name, a.Type.ToString(), a.Hours, a.Amount))
                .ToList()
        )).ToList();

        return Results.Ok(new PagedResult<TimeEntryListItemDto>(items, totalCount, page, pageSize));
    }

    // ─── Admin Day Detail ─────────────────────────────────────────

    private static async Task<IResult> GetDayDetail(
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        ClaimsPrincipal user,
        CancellationToken ct,
        Guid personId,
        string date)
    {
        var (currentPersonId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null) return Results.Unauthorized();
        if (!await IsAdminOrProjectLeader(currentPersonId!.Value, tenantProvider.TenantId.Value, db, ct))
            return Results.Forbid();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == currentPersonId!.Value && m.State == TenantMembershipState.Active, ct);
        if (membership is null || membership.Role != TenantRole.TenantAdmin) return Results.Forbid();

        if (!DateOnly.TryParse(date, out var parsedDate)) return Results.BadRequest(new { error = "Ugyldig dato." });

        var tenantId = tenantProvider.TenantId.Value;
        var employeeName = await db.Persons.Where(p => p.Id == personId).Select(p => p.FullName).FirstOrDefaultAsync(ct) ?? "";

        var rawEntries = await db.TimeEntries
            .Where(t => t.TenantId == tenantId && t.PersonId == personId && t.Date == parsedDate)
            .OrderBy(t => t.StartTime)
            .Select(t => new { t.Id, t.Date, t.Hours, t.OvertimeHours, t.BreakMinutes, t.Category, t.Status,
                t.ProjectId, t.JobId, t.StartTime, t.EndTime, t.Notes, t.IsManual })
            .ToListAsync(ct);

        var projectIds = rawEntries.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).Distinct().ToList();
        var jobIds = rawEntries.Where(e => e.JobId.HasValue).Select(e => e.JobId!.Value).Distinct().ToList();
        var projectNames = projectIds.Count > 0
            ? await db.Projects.Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();
        var jobDescs = jobIds.Count > 0
            ? await db.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.Description ?? "", ct)
            : new Dictionary<Guid, string>();

        var entryIds = rawEntries.Select(e => e.Id).ToList();
        var allowances = entryIds.Count > 0
            ? await db.TimeEntryAllowances.Where(a => entryIds.Contains(a.TimeEntryId))
                .Join(db.AllowanceRules, a => a.AllowanceRuleId, r => r.Id,
                    (a, r) => new { a.TimeEntryId, r.Name, r.Type, a.Hours, a.Amount })
                .ToListAsync(ct)
            : [];

        var entries = rawEntries.Select(t => new TimeEntryListItemDto(
            t.Id, t.Date, t.Hours, t.OvertimeHours, t.BreakMinutes,
            CategoryToString(t.Category), StatusToString(t.Status), employeeName,
            t.ProjectId, t.ProjectId.HasValue && projectNames.TryGetValue(t.ProjectId.Value, out var pn) ? pn : null,
            t.JobId, t.JobId.HasValue && jobDescs.TryGetValue(t.JobId.Value, out var jd) ? jd : null,
            t.StartTime, t.EndTime, t.Notes, t.IsManual,
            allowances.Where(a => a.TimeEntryId == t.Id)
                .Select(a => new TimeEntryAllowanceTagDto(a.Name, a.Type.ToString(), a.Hours, a.Amount)).ToList()
        )).ToList();

        var dayStatus = entries.Count == 0 ? "Mangler"
            : entries.All(e => e.Status == "Godkjent") ? "Godkjent"
            : "Registrert";

        return Results.Ok(new DayDetailDto(
            employeeName, parsedDate, entries.Sum(e => e.Hours), entries.Sum(e => e.OvertimeHours),
            dayStatus, entries));
    }

    // ─── Admin Batch Approve Day ────────────────────────────────────

    private static async Task<IResult> ApproveDayEntries(
        ApproveDayRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (currentPersonId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null) return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == currentPersonId!.Value && m.State == TenantMembershipState.Active, ct);
        if (membership is null || membership.Role != TenantRole.TenantAdmin) return Results.Forbid();

        var entries = await db.TimeEntries
            .Where(t => t.TenantId == tenantProvider.TenantId.Value
                && t.PersonId == request.PersonId
                && t.Date == request.Date
                && t.Status == TimeEntryStatus.Submitted)
            .ToListAsync(ct);

        foreach (var entry in entries)
            entry.Status = TimeEntryStatus.Approved;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { approved = entries.Count });
    }

    // ─── My Schedule ────────────────────────────────────────────────

    private static async Task<IResult> GetMySchedule(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var assignment = await db.EmployeeScheduleAssignments
            .Where(a => a.PersonId == personId!.Value
                && a.EffectiveFrom <= today
                && (a.EffectiveTo == null || a.EffectiveTo >= today))
            .OrderByDescending(a => a.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (assignment is null)
            return Results.Ok(new MyScheduleDto(null, null, 37.5m, 30));

        var schedule = await db.WorkSchedules
            .Where(s => s.Id == assignment.WorkScheduleId)
            .Select(s => new MyScheduleDto(s.Id, s.Name, s.WeeklyHours, s.DefaultBreakMinutes))
            .FirstOrDefaultAsync(ct);

        return Results.Ok(schedule ?? new MyScheduleDto(null, null, 37.5m, 30));
    }

    // ─── Add Allowance to Time Entry ────────────────────────────────

    private static async Task<IResult> AddTimeEntryAllowance(
        Guid id,
        AddTimeEntryAllowanceRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == id && t.PersonId == personId!.Value, ct);

        if (entry is null)
            return Results.NotFound();

        var rule = await db.AllowanceRules
            .FirstOrDefaultAsync(r => r.Id == request.AllowanceRuleId && r.IsActive, ct);

        if (rule is null)
            return Results.BadRequest(new { error = "Tilleggsregel ikke funnet." });

        if (rule.Type != AllowanceType.OperationBased)
            return Results.BadRequest(new { error = "Kun operasjonsbaserte tillegg kan registreres manuelt." });

        var allowance = new TimeEntryAllowance
        {
            TimeEntryId = id,
            AllowanceRuleId = rule.Id,
            Hours = request.Hours ?? 0,
            Amount = rule.Amount,
            Notes = request.Notes
        };

        db.TimeEntryAllowances.Add(allowance);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hours/{id}/allowances/{allowance.Id}",
            new { id = allowance.Id });
    }

    // ─── Heatmap ────────────────────────────────────────────────────

    private static async Task<IResult> GetHoursHeatmap(
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        ClaimsPrincipal user,
        CancellationToken ct,
        string? from = null,
        string? to = null)
    {
        var (currentPersonId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m =>
                m.PersonId == currentPersonId!.Value &&
                m.State == TenantMembershipState.Active, ct);

        if (membership is null || membership.Role != TenantRole.TenantAdmin)
            return Results.Forbid();

        var fromDate = DateOnly.TryParse(from, out var f) ? f : DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-28);
        var toDate = DateOnly.TryParse(to, out var t2) ? t2 : DateOnly.FromDateTime(DateTime.UtcNow);

        var tenantId = tenantProvider.TenantId.Value;

        var entries = await db.TimeEntries
            .Where(e => e.TenantId == tenantId && e.Date >= fromDate && e.Date <= toDate)
            .Select(e => new { e.PersonId, e.Date, e.Hours, e.OvertimeHours, e.Status })
            .ToListAsync(ct);

        var absences = await db.Absences
            .Where(a => a.TenantId == tenantId && a.StartDate <= toDate && a.EndDate >= fromDate
                && a.Status != AbsenceStatus.Rejected)
            .Select(a => new { a.PersonId, a.Type, a.StartDate, a.EndDate })
            .ToListAsync(ct);

        var activeMembers = await db.TenantMemberships
            .Where(m => m.TenantId == tenantId && m.State == TenantMembershipState.Active)
            .Select(m => m.PersonId)
            .ToListAsync(ct);

        var personNames = await db.Persons
            .Where(p => activeMembers.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var dates = new List<DateOnly>();
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
            dates.Add(d);

        var rows = activeMembers
            .Where(pid => personNames.ContainsKey(pid))
            .Select(pid => new HeatmapEmployeeRow(
                pid,
                personNames[pid],
                dates.Select(date =>
                {
                    var dayEntries = entries.Where(e => e.PersonId == pid && e.Date == date).ToList();
                    var dayAbsence = absences.FirstOrDefault(a => a.PersonId == pid && a.StartDate <= date && a.EndDate >= date);
                    var totalHours = dayEntries.Sum(e => e.Hours);
                    var overtime = dayEntries.Sum(e => e.OvertimeHours);
                    var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

                    string status;
                    string? absType = null;

                    if (dayAbsence is not null)
                    {
                        status = "Fravær";
                        absType = AbsenceTypeToShort(dayAbsence.Type);
                    }
                    else if (dayEntries.Count == 0)
                        status = (isWeekend || date >= DateOnly.FromDateTime(DateTime.Today)) ? "" : "Mangler";
                    else if (dayEntries.All(e => e.Status == TimeEntryStatus.Approved))
                        status = "Godkjent";
                    else
                        status = "Registrert";

                    return new HeatmapCell(date, totalHours, overtime, status, absType);
                }).ToList()))
            .OrderBy(r => r.EmployeeName)
            .ToList();

        return Results.Ok(new HoursHeatmapDto(rows, dates));
    }

    // ─── My Heatmap (Employee) ──────────────────────────────────────

    private static async Task<IResult> GetMyHeatmap(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct,
        string? from = null,
        string? to = null)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var fromDate = DateOnly.TryParse(from, out var f) ? f : DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-28);
        var toDate = DateOnly.TryParse(to, out var t2) ? t2 : DateOnly.FromDateTime(DateTime.UtcNow);

        var entries = await db.TimeEntries
            .Where(e => e.PersonId == personId!.Value && e.Date >= fromDate && e.Date <= toDate)
            .Select(e => new { e.Date, e.Hours, e.Status })
            .ToListAsync(ct);

        // Load absences covering this period
        var absences = await db.Absences
            .Where(a => a.PersonId == personId!.Value && a.StartDate <= toDate && a.EndDate >= fromDate
                && a.Status != AbsenceStatus.Rejected)
            .Select(a => new { a.Type, a.StartDate, a.EndDate })
            .ToListAsync(ct);

        var dates = new List<DateOnly>();
        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
            dates.Add(d);

        var cells = dates.Select(date =>
        {
            var dayEntries = entries.Where(e => e.Date == date).ToList();
            var dayAbsence = absences.FirstOrDefault(a => a.StartDate <= date && a.EndDate >= date);
            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            string status;
            string? absenceType = null;

            if (dayAbsence is not null)
            {
                status = "Fravær";
                absenceType = AbsenceTypeToShort(dayAbsence.Type);
            }
            else if (dayEntries.Count == 0)
                status = (isWeekend || date >= DateOnly.FromDateTime(DateTime.Today)) ? "" : "Mangler";
            else if (dayEntries.All(e => e.Status == TimeEntryStatus.Approved))
                status = "Godkjent";
            else
                status = "Registrert";

            return new MyHeatmapCell(date, dayEntries.Sum(e => e.Hours), status, absenceType);
        }).ToList();

        return Results.Ok(new MyHeatmapDto(dates, cells));
    }

    // ─── Overtime Bank ───────────────────────────────────────────────

    private static async Task<IResult> CreditOvertimeBank(
        CreditOvertimeBankRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null) return Results.Unauthorized();

        if (request.Hours <= 0)
            return Results.BadRequest(new { error = "Timer ma vaere storre enn 0." });

        var entry = await db.TimeEntries
            .FirstOrDefaultAsync(t => t.Id == request.TimeEntryId && t.PersonId == personId!.Value, ct);

        if (entry is null) return Results.NotFound();

        var bankEntry = new OvertimeBankEntry
        {
            TenantId = tenantProvider.TenantId.Value,
            PersonId = personId!.Value,
            Date = entry.Date,
            Hours = request.Hours,
            Action = OvertimeBankAction.Credited,
            Description = $"Overtid {entry.Date:dd.MM.yyyy}",
            TimeEntryId = entry.Id
        };

        db.OvertimeBankEntries.Add(bankEntry);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hours/overtime-bank/{bankEntry.Id}", new { id = bankEntry.Id });
    }

    // ─── Schedules ──────────────────────────────────────────────────

    private static async Task<IResult> ListSchedules(
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var schedules = await db.WorkSchedules
            .Where(s => s.TenantId == tenantProvider.TenantId.Value)
            .OrderBy(s => s.Name)
            .Select(s => new WorkScheduleDto(
                s.Id,
                s.Name,
                s.WeeklyHours,
                s.DefaultBreakMinutes,
                s.IsActive))
            .ToListAsync(ct);

        return Results.Ok(schedules);
    }

    private static async Task<IResult> CreateSchedule(
        CreateScheduleRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (request.WeeklyHours <= 0)
            return Results.BadRequest(new { error = "Uketimer må være større enn 0." });

        var schedule = new WorkSchedule
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            WeeklyHours = request.WeeklyHours,
            DefaultBreakMinutes = request.DefaultBreakMinutes,
            IsActive = true
        };

        db.WorkSchedules.Add(schedule);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/schedules/{schedule.Id}", new { id = schedule.Id });
    }

    // ─── Allowance Rules ────────────────────────────────────────────

    private static async Task<IResult> ListAllowanceRules(
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var rules = await db.AllowanceRules
            .Where(r => r.TenantId == tenantProvider.TenantId.Value)
            .OrderBy(r => r.Name)
            .Select(r => new AllowanceRuleDto(
                r.Id,
                r.Name,
                r.Type.ToString(),
                r.AmountType.ToString(),
                r.Amount,
                r.TimeRangeStart,
                r.TimeRangeEnd,
                r.IsActive))
            .ToListAsync(ct);

        return Results.Ok(rules);
    }

    private static async Task<IResult> CreateAllowanceRule(
        AllowanceRuleDto request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        var type = request.Type?.ToLowerInvariant() switch
        {
            "timebased" => AllowanceType.TimeBased,
            "daybased" => AllowanceType.DayBased,
            "operationbased" => AllowanceType.OperationBased,
            "fixedperday" => AllowanceType.FixedPerDay,
            _ => (AllowanceType?)null
        };

        if (type is null)
            return Results.BadRequest(new { error = $"Ugyldig type: {request.Type}" });

        var amountType = request.AmountType?.ToLowerInvariant() switch
        {
            "fixedkroner" => AllowanceAmountType.FixedKroner,
            "percentage" => AllowanceAmountType.Percentage,
            _ => (AllowanceAmountType?)null
        };

        if (amountType is null)
            return Results.BadRequest(new { error = $"Ugyldig beløpstype: {request.AmountType}" });

        var rule = new AllowanceRule
        {
            TenantId = tenantProvider.TenantId.Value,
            Name = request.Name,
            Type = type.Value,
            AmountType = amountType.Value,
            Amount = request.Amount,
            TimeRangeStart = request.TimeRangeStart,
            TimeRangeEnd = request.TimeRangeEnd,
            IsActive = true
        };

        db.AllowanceRules.Add(rule);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/allowances/rules/{rule.Id}", new { id = rule.Id });
    }

    private static async Task<IResult> UpdateAllowanceRule(
        Guid id,
        AllowanceRuleDto request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var rule = await db.AllowanceRules
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantProvider.TenantId.Value, ct);
        if (rule is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name)) rule.Name = request.Name;
        rule.Amount = request.Amount;
        rule.IsActive = request.IsActive;
        rule.TimeRangeStart = request.TimeRangeStart;
        rule.TimeRangeEnd = request.TimeRangeEnd;

        if (request.Type is not null)
        {
            var type = request.Type.ToLowerInvariant() switch
            {
                "timebased" => AllowanceType.TimeBased,
                "daybased" => AllowanceType.DayBased,
                "operationbased" => AllowanceType.OperationBased,
                "fixedperday" => AllowanceType.FixedPerDay,
                _ => (AllowanceType?)null
            };
            if (type.HasValue) rule.Type = type.Value;
        }

        if (request.AmountType is not null)
        {
            var amountType = request.AmountType.ToLowerInvariant() switch
            {
                "fixedkroner" => AllowanceAmountType.FixedKroner,
                "percentage" => AllowanceAmountType.Percentage,
                _ => (AllowanceAmountType?)null
            };
            if (amountType.HasValue) rule.AmountType = amountType.Value;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteAllowanceRule(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var rule = await db.AllowanceRules
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantProvider.TenantId.Value, ct);
        if (rule is null) return Results.NotFound();

        rule.IsDeleted = true;
        rule.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
