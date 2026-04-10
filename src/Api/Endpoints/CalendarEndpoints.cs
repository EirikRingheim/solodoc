using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Calendar;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Calendar;

namespace Solodoc.Api.Endpoints;

public static class CalendarEndpoints
{
    public static WebApplication MapCalendarEndpoints(this WebApplication app)
    {
        app.MapGet("/api/calendar/events", ListEvents).RequireAuthorization();
        app.MapPost("/api/calendar/events", CreateEvent).RequireAuthorization();
        app.MapPut("/api/calendar/events/{id:guid}", UpdateEvent).RequireAuthorization();
        app.MapDelete("/api/calendar/events/{id:guid}", DeleteEvent).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ListEvents(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        Guid.TryParse(personIdClaim, out var personId);

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        // Regular calendar events
        var query = db.CalendarEvents.Where(e => e.TenantId == tenantProvider.TenantId.Value).AsQueryable();
        if (from.HasValue)
            query = query.Where(e => e.StartAt >= from.Value || (e.EndAt != null && e.EndAt >= from.Value));
        if (to.HasValue)
            query = query.Where(e => e.StartAt <= to.Value);

        var events = await query
            .OrderBy(e => e.StartAt)
            .Select(e => new CalendarEventDto(
                e.Id, e.Title, e.Description, e.StartAt, e.EndAt, e.IsAllDay, e.Location))
            .ToListAsync(ct);

        // Add planner entries as calendar events
        if (personId != Guid.Empty && tenantProvider.TenantId.HasValue)
        {
            var fromDate = from.HasValue ? DateOnly.FromDateTime(from.Value.UtcDateTime) : DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
            var toDate = to.HasValue ? DateOnly.FromDateTime(to.Value.UtcDateTime) : DateOnly.FromDateTime(DateTime.Today.AddDays(60));

            var plannerDays = await db.PlannerEntries
                .Where(p => p.TenantId == tenantProvider.TenantId.Value && p.PersonId == personId
                    && p.Date >= fromDate && p.Date <= toDate)
                .Select(p => new { p.Id, p.Date, p.ShiftDefinitionId, p.ProjectId })
                .ToListAsync(ct);

            if (plannerDays.Count > 0)
            {
                var shiftIds = plannerDays.Select(p => p.ShiftDefinitionId).Distinct().ToList();
                var shifts = await db.ShiftDefinitions
                    .Where(s => shiftIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s, ct);

                var projIds = plannerDays.Where(p => p.ProjectId.HasValue).Select(p => p.ProjectId!.Value).Distinct().ToList();
                var projNames = projIds.Count > 0
                    ? await db.Projects.Where(p => projIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
                    : new Dictionary<Guid, string>();

                foreach (var day in plannerDays)
                {
                    if (!shifts.TryGetValue(day.ShiftDefinitionId, out var shift)) continue;
                    if (!shift.IsWorkDay) continue; // Don't show off-days

                    var projName = day.ProjectId.HasValue && projNames.TryGetValue(day.ProjectId.Value, out var pn) ? pn : null;
                    var title = projName is not null ? $"{shift.Name} — {projName}" : shift.Name;

                    var startAt = new DateTimeOffset(day.Date.ToDateTime(shift.StartTime ?? new TimeOnly(8, 0)), TimeSpan.Zero);
                    var endAt = new DateTimeOffset(day.Date.ToDateTime(shift.EndTime ?? new TimeOnly(16, 0)), TimeSpan.Zero);

                    events.Add(new CalendarEventDto(day.Id, title, null, startAt, endAt, false, null));
                }
            }

            // Add absences as calendar events
            var absences = await db.Absences
                .Where(a => a.PersonId == personId && a.StartDate <= toDate && a.EndDate >= fromDate
                    && a.Status != AbsenceStatus.Rejected)
                .ToListAsync(ct);

            foreach (var abs in absences)
            {
                var absTitle = abs.Type switch
                {
                    AbsenceType.Ferie => "Ferie",
                    AbsenceType.Sykmelding => "Sykmelding",
                    AbsenceType.Egenmelding => "Egenmelding",
                    AbsenceType.Lege => "Lege",
                    AbsenceType.Tannlege => "Tannlege",
                    AbsenceType.Foreldrepermisjon => "Foreldrepermisjon",
                    AbsenceType.Avspasering => "Avspasering",
                    _ => "Fravaer"
                };

                var absStart = new DateTimeOffset(abs.StartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                var absEnd = new DateTimeOffset(abs.EndDate.ToDateTime(new TimeOnly(23, 59)), TimeSpan.Zero);

                events.Add(new CalendarEventDto(abs.Id, absTitle, abs.Notes, absStart, absEnd, true, null));
            }
        }

        return Results.Ok(events.OrderBy(e => e.StartAt).ToList());
    }

    private static async Task<IResult> CreateEvent(
        CreateCalendarEventRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "Tittel er påkrevd." });

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId) || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var calendarEvent = new CalendarEvent
        {
            TenantId = tenantProvider.TenantId.Value,
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            IsAllDay = request.IsAllDay,
            Location = request.Location,
            CreatedById = personId
        };

        db.CalendarEvents.Add(calendarEvent);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/calendar/events/{calendarEvent.Id}", new { id = calendarEvent.Id });
    }

    private static async Task<IResult> UpdateEvent(
        Guid id,
        CreateCalendarEventRequest request,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "Tittel er påkrevd." });

        var calendarEvent = await db.CalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantProvider.TenantId.Value, ct);
        if (calendarEvent is null)
            return Results.NotFound();

        calendarEvent.Title = request.Title;
        calendarEvent.Description = request.Description;
        calendarEvent.StartAt = request.StartAt;
        calendarEvent.EndAt = request.EndAt;
        calendarEvent.IsAllDay = request.IsAllDay;
        calendarEvent.Location = request.Location;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteEvent(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        var calendarEvent = await db.CalendarEvents
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantProvider.TenantId.Value, ct);
        if (calendarEvent is null)
            return Results.NotFound();

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        Guid.TryParse(personIdClaim, out var personId);

        calendarEvent.IsDeleted = true;
        calendarEvent.DeletedAt = DateTimeOffset.UtcNow;
        calendarEvent.DeletedBy = personId;

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
