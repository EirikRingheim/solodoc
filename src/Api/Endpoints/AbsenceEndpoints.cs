using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Hours;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Hours;

namespace Solodoc.Api.Endpoints;

public static class AbsenceEndpoints
{
    public static WebApplication MapAbsenceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/absences", ListMyAbsences).RequireAuthorization();
        app.MapPost("/api/absences", CreateAbsence).RequireAuthorization();
        app.MapDelete("/api/absences/{id:guid}", DeleteAbsence).RequireAuthorization();
        app.MapGet("/api/hours/balance", GetMyBalance).RequireAuthorization();
        app.MapGet("/api/hours/balance/{personId:guid}", GetEmployeeBalance).RequireAuthorization();
        app.MapPatch("/api/absences/{id:guid}/approve", ApproveAbsence).RequireAuthorization();
        app.MapPatch("/api/absences/{id:guid}/reject", RejectAbsence).RequireAuthorization();

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

    private static string AbsenceTypeToString(AbsenceType t) => t switch
    {
        AbsenceType.Ferie => "Ferie",
        AbsenceType.Sykmelding => "Sykmelding",
        AbsenceType.Egenmelding => "Egenmelding",
        AbsenceType.Lege => "Lege",
        AbsenceType.Tannlege => "Tannlege",
        AbsenceType.Foreldrepermisjon => "Foreldrepermisjon",
        AbsenceType.Permisjon => "Permisjon",
        AbsenceType.Annet => "Annet",
        _ => "Annet"
    };

    private static AbsenceType ParseAbsenceType(string? s) => s?.ToLowerInvariant() switch
    {
        "ferie" => AbsenceType.Ferie,
        "sykmelding" => AbsenceType.Sykmelding,
        "egenmelding" => AbsenceType.Egenmelding,
        "lege" => AbsenceType.Lege,
        "tannlege" => AbsenceType.Tannlege,
        "foreldrepermisjon" => AbsenceType.Foreldrepermisjon,
        "permisjon" => AbsenceType.Permisjon,
        "avspasering" => AbsenceType.Avspasering,
        _ => AbsenceType.Annet
    };

    private static string StatusToString(AbsenceStatus s) => s switch
    {
        AbsenceStatus.Registered => "Registrert",
        AbsenceStatus.Pending => "Sokt",
        AbsenceStatus.Approved => "Godkjent",
        AbsenceStatus.Rejected => "Avvist",
        _ => "Registrert"
    };

    private static async Task<IResult> ListMyAbsences(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct,
        int? year = null,
        string? from = null,
        string? to = null)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var query = db.Absences.Where(a => a.PersonId == personId!.Value);

        if (year.HasValue)
            query = query.Where(a => a.StartDate.Year == year.Value);

        if (DateOnly.TryParse(from, out var fromDate) && DateOnly.TryParse(to, out var toDate))
            query = query.Where(a => a.EndDate >= fromDate && a.StartDate <= toDate);

        var absences = await query
            .OrderByDescending(a => a.StartDate)
            .Select(a => new AbsenceListItemDto(
                a.Id,
                AbsenceTypeToString(a.Type),
                a.StartDate,
                a.EndDate,
                a.Hours,
                a.Notes,
                StatusToString(a.Status),
                null))
            .ToListAsync(ct);

        return Results.Ok(absences);
    }

    private static async Task<IResult> CreateAbsence(
        CreateAbsenceRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (request.StartDate > request.EndDate)
            return Results.BadRequest(new { error = "Startdato ma vaere for eller lik sluttdato." });

        // Check for conflicting time entries on any day in the range
        var conflictDays = new List<DateOnly>();
        for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            var hasHours = await db.TimeEntries
                .AnyAsync(t => t.PersonId == personId!.Value && t.Date == d, ct);
            if (hasHours) conflictDays.Add(d);
        }

        if (conflictDays.Count > 0)
        {
            var dayList = string.Join(", ", conflictDays.Select(d => d.ToString("dd.MM")));
            return Results.BadRequest(new { error = $"Du har allerede timer registrert pa: {dayList}. Slett timene forst, og registrer fravær etterpå." });
        }

        // Check for existing absence on these days
        var hasAbsence = await db.Absences
            .AnyAsync(a => a.PersonId == personId!.Value
                && a.Status != AbsenceStatus.Rejected
                && a.StartDate <= request.EndDate && a.EndDate >= request.StartDate, ct);
        if (hasAbsence)
            return Results.BadRequest(new { error = "Du har allerede registrert fravær i denne perioden." });

        var absenceType = ParseAbsenceType(request.Type);

        // Future ferie/avspasering = application (Søkt), past/today sick = instant
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var isFutureLeave = request.StartDate > today
            && absenceType is AbsenceType.Ferie or AbsenceType.Avspasering;

        var absence = new Absence
        {
            TenantId = tenantProvider.TenantId.Value,
            PersonId = personId!.Value,
            Type = absenceType,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Hours = request.Hours,
            Notes = request.Notes,
            Status = isFutureLeave ? AbsenceStatus.Pending : AbsenceStatus.Registered
        };

        db.Absences.Add(absence);

        // If avspasering and not pending (past = instant deduct), deduct from overtime bank
        if (absence.Type == AbsenceType.Avspasering && absence.Hours > 0 && !isFutureLeave)
        {
            var bankBalance = await db.OvertimeBankEntries
                .Where(o => o.PersonId == personId!.Value && o.TenantId == tenantProvider.TenantId.Value)
                .SumAsync(o => o.Hours, ct);

            if (bankBalance < absence.Hours)
                return Results.BadRequest(new { error = $"Ikke nok timer i timebanken. Saldo: {bankBalance:0.#}t" });

            db.OvertimeBankEntries.Add(new OvertimeBankEntry
            {
                TenantId = tenantProvider.TenantId.Value,
                PersonId = personId!.Value,
                Date = absence.StartDate,
                Hours = -absence.Hours, // Negative = debit
                Action = OvertimeBankAction.UsedAsTimeOff,
                Description = $"Avspasering {absence.StartDate:dd.MM}–{absence.EndDate:dd.MM}"
            });
        }

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/absences/{absence.Id}", new { id = absence.Id });
    }

    private static async Task<IResult> DeleteAbsence(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var absence = await db.Absences
            .FirstOrDefaultAsync(a => a.Id == id && a.PersonId == personId!.Value, ct);

        if (absence is null) return Results.NotFound();
        if (absence.Status == AbsenceStatus.Approved)
            return Results.BadRequest(new { error = "Godkjent fravær kan ikke slettes." });

        db.Absences.Remove(absence);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ApproveAbsence(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (currentId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null) return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == currentId!.Value && m.State == TenantMembershipState.Active, ct);
        if (membership is null || membership.Role != TenantRole.TenantAdmin) return Results.Forbid();

        var absence = await db.Absences.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (absence is null) return Results.NotFound();
        if (absence.Status != AbsenceStatus.Pending)
            return Results.BadRequest(new { error = "Kun soknader kan godkjennes." });

        absence.Status = AbsenceStatus.Approved;

        // If avspasering approved, deduct from overtime bank now
        if (absence.Type == AbsenceType.Avspasering && absence.Hours > 0)
        {
            var bankBalance = await db.OvertimeBankEntries
                .Where(o => o.PersonId == absence.PersonId && o.TenantId == absence.TenantId)
                .SumAsync(o => o.Hours, ct);

            if (bankBalance < absence.Hours)
                return Results.BadRequest(new { error = $"Ikke nok timer i timebanken. Saldo: {bankBalance:0.#}t" });

            db.OvertimeBankEntries.Add(new OvertimeBankEntry
            {
                TenantId = absence.TenantId,
                PersonId = absence.PersonId,
                Date = absence.StartDate,
                Hours = -absence.Hours,
                Action = OvertimeBankAction.UsedAsTimeOff,
                Description = $"Avspasering {absence.StartDate:dd.MM}–{absence.EndDate:dd.MM}"
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { status = "Godkjent" });
    }

    private static async Task<IResult> RejectAbsence(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var (currentId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == currentId!.Value && m.State == TenantMembershipState.Active, ct);
        if (membership is null || membership.Role != TenantRole.TenantAdmin) return Results.Forbid();

        var absence = await db.Absences.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (absence is null) return Results.NotFound();
        if (absence.Status != AbsenceStatus.Pending)
            return Results.BadRequest(new { error = "Kun soknader kan avvises." });

        absence.Status = AbsenceStatus.Rejected;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { status = "Avvist" });
    }

    private static async Task<IResult> GetMyBalance(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        return Results.Ok(await CalcBalance(personId!.Value, tenantProvider.TenantId, db, ct));
    }

    private static async Task<IResult> GetEmployeeBalance(
        Guid personId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        // Admin only
        var (currentId, valid) = GetPersonId(user);
        if (!valid || tenantProvider.TenantId is null) return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .FirstOrDefaultAsync(m => m.PersonId == currentId!.Value && m.State == TenantMembershipState.Active, ct);
        if (membership is null || membership.Role != TenantRole.TenantAdmin)
            return Results.Forbid();

        return Results.Ok(await CalcBalance(personId, tenantProvider.TenantId, db, ct));
    }

    private static async Task<BalanceSummaryDto> CalcBalance(
        Guid personId, Guid? tenantId, SolodocDbContext db, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;

        // Vacation: from VacationBalance if exists, otherwise default
        var vacBalance = await db.VacationBalances
            .FirstOrDefaultAsync(v => v.PersonId == personId && v.Year == year, ct);

        var vacAllowanceHours = vacBalance is not null
            ? vacBalance.AnnualAllowanceDays * 7.5m + vacBalance.CarriedOverDays * 7.5m
            : 187.5m; // Default: 25 days * 7.5h

        // Vacation used: sum of Ferie absences this year
        var vacUsedHours = await db.Absences
            .Where(a => a.PersonId == personId && a.Type == AbsenceType.Ferie
                && a.StartDate.Year == year && a.Status != AbsenceStatus.Rejected)
            .SumAsync(a => a.Hours, ct);

        // Sick leave hours this year
        var sickHours = await db.Absences
            .Where(a => a.PersonId == personId
                && (a.Type == AbsenceType.Sykmelding || a.Type == AbsenceType.Egenmelding)
                && a.StartDate.Year == year)
            .SumAsync(a => a.Hours, ct);

        // Overtime bank
        var bankBalance = tenantId.HasValue
            ? await db.OvertimeBankEntries
                .Where(o => o.PersonId == personId && o.TenantId == tenantId.Value)
                .SumAsync(o => o.Hours, ct)
            : 0;

        // TODO: tenant setting for timebank enabled
        var timebankEnabled = true;

        return new BalanceSummaryDto(
            vacAllowanceHours,
            vacUsedHours,
            vacAllowanceHours - vacUsedHours,
            sickHours,
            bankBalance,
            timebankEnabled);
    }
}
