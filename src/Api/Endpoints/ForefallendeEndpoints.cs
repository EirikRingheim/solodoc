using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Forefallende;

namespace Solodoc.Api.Endpoints;

public static class ForefallendeEndpoints
{
    public static WebApplication MapForefallendeEndpoints(this WebApplication app)
    {
        app.MapGet("/api/forefallende", GetItems).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetItems(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId))
            return Results.Unauthorized();

        var membership = await db.TenantMemberships
            .Where(m => m.PersonId == personId && m.State == TenantMembershipState.Active)
            .FirstOrDefaultAsync(ct);

        if (membership is null)
            return Results.Ok(new List<ForefallendItemDto>());

        var tenantId = membership.TenantId;
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var ninetyDaysFromNow = today.AddDays(90);

        var items = new List<ForefallendItemDto>();

        // 1. Open/overdue deviations with deadlines
        var deviations = await db.Deviations
            .Where(d => d.TenantId == tenantId
                        && d.Status != DeviationStatus.Closed
                        && d.CorrectiveActionDeadline != null)
            .Select(d => new
            {
                d.Id,
                d.Title,
                ProjectName = db.Projects
                    .Where(p => p.Id == d.ProjectId)
                    .Select(p => p.Name)
                    .FirstOrDefault(),
                DueDate = d.CorrectiveActionDeadline!.Value,
                Status = d.Status == DeviationStatus.Open ? "Åpen" : "Under behandling"
            })
            .ToListAsync(ct);

        foreach (var d in deviations)
        {
            var subtitle = d.ProjectName is not null
                ? $"Avvik ({d.Status}) - {d.ProjectName}"
                : $"Avvik ({d.Status})";
            items.Add(new ForefallendItemDto(
                d.Id, "deviation", d.Title, subtitle,
                d.DueDate, Categorize(d.DueDate, now)));
        }

        // 2. Expiring certifications (within 90 days)
        var certs = await db.EmployeeCertifications
            .Where(c => c.TenantId == tenantId
                        && c.ExpiryDate != null
                        && c.ExpiryDate.Value <= ninetyDaysFromNow)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Type,
                c.ExpiryDate,
                PersonName = db.Persons.Where(p => p.Id == c.PersonId).Select(p => p.FullName).FirstOrDefault()
            })
            .ToListAsync(ct);

        foreach (var c in certs)
        {
            var expiryOffset = new DateTimeOffset(c.ExpiryDate!.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var subtitle = c.PersonName is not null
                ? $"Sertifikat - {c.PersonName}"
                : "Sertifikat";
            items.Add(new ForefallendItemDto(
                c.Id, "certification", $"{c.Name} ({c.Type})", subtitle,
                expiryOffset, Categorize(expiryOffset, now)));
        }

        // 3. Upcoming calendar events (next 90 days)
        var calendarCutoff = now.AddDays(90);
        var calendarEvents = await db.CalendarEvents
            .Where(e => e.TenantId == tenantId && e.StartAt <= calendarCutoff && e.StartAt >= now.AddDays(-30))
            .Select(e => new { e.Id, e.Title, e.StartAt, e.Location })
            .ToListAsync(ct);

        foreach (var e in calendarEvents)
        {
            var subtitle = e.Location is not null ? $"Kalender - {e.Location}" : "Kalender";
            items.Add(new ForefallendItemDto(
                e.Id, "calendar", e.Title, subtitle,
                e.StartAt, Categorize(e.StartAt, now)));
        }

        // 4. Overdue/upcoming safety rounds
        var safetyRounds = await db.SafetyRoundSchedules
            .Where(s => s.TenantId == tenantId
                        && s.IsActive
                        && s.NextDue <= ninetyDaysFromNow)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.NextDue,
                ProjectName = s.ProjectId != null
                    ? db.Projects.Where(p => p.Id == s.ProjectId).Select(p => p.Name).FirstOrDefault()
                    : null
            })
            .ToListAsync(ct);

        foreach (var s in safetyRounds)
        {
            var dueOffset = new DateTimeOffset(s.NextDue.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var subtitle = s.ProjectName is not null
                ? $"Vernerunde - {s.ProjectName}"
                : "Vernerunde";
            items.Add(new ForefallendItemDto(
                s.Id, "safety-round", s.Name, subtitle,
                dueOffset, Categorize(dueOffset, now)));
        }

        // 5. HMS meetings
        var hmsCutoff = DateOnly.FromDateTime(now.AddDays(90).UtcDateTime);
        var hmsMeetings = await db.HmsMeetings
            .Where(m => m.TenantId == tenantId
                        && m.Date <= hmsCutoff
                        && m.Date >= today.AddDays(-30))
            .Select(m => new { m.Id, m.Title, m.Date, m.Location })
            .ToListAsync(ct);

        foreach (var m in hmsMeetings)
        {
            var dueOffset = new DateTimeOffset(m.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var subtitle = m.Location is not null ? $"HMS-møte - {m.Location}" : "HMS-møte";
            items.Add(new ForefallendItemDto(
                m.Id, "hms-meeting", m.Title, subtitle,
                dueOffset, Categorize(dueOffset, now)));
        }

        // Sort: overdue first (most overdue), then by due date ascending
        items = items
            .OrderBy(i => i.Column switch
            {
                "overdue" => 0,
                "this-week" => 1,
                "this-month" => 2,
                _ => 3
            })
            .ThenBy(i => i.DueDate)
            .ToList();

        return Results.Ok(items);
    }

    private static string Categorize(DateTimeOffset dueDate, DateTimeOffset now)
    {
        if (dueDate < now)
            return "overdue";

        // Calculate week boundaries (Monday to Sunday)
        var today = now.UtcDateTime.Date;
        var dayOfWeek = today.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)today.DayOfWeek - 1;
        var endOfWeek = today.AddDays(6 - dayOfWeek).AddHours(23).AddMinutes(59).AddSeconds(59);

        if (dueDate.UtcDateTime <= endOfWeek)
            return "this-week";

        // End of month
        var endOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month), 23, 59, 59);
        if (dueDate.UtcDateTime <= endOfMonth)
            return "this-month";

        return "later";
    }
}
