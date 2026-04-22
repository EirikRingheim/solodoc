using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Hms;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Hms;

namespace Solodoc.Api.Endpoints;

public static class HmsEndpoints
{
    public static WebApplication MapHmsEndpoints(this WebApplication app)
    {
        // SJA
        app.MapGet("/api/hms/sja", ListSjaForms).RequireAuthorization();
        app.MapPost("/api/hms/sja", CreateSjaForm).RequireAuthorization();
        app.MapGet("/api/hms/sja/{id:guid}", GetSjaFormDetail).RequireAuthorization();
        app.MapPost("/api/hms/sja/{id:guid}/hazards", AddHazard).RequireAuthorization();
        app.MapPost("/api/hms/sja/{id:guid}/participants", AddParticipant).RequireAuthorization();
        app.MapPost("/api/hms/sja/{id:guid}/participants/external", AddExternalParticipant).RequireAuthorization();
        app.MapDelete("/api/hms/sja/{id:guid}/participants/{participantId:guid}", RemoveParticipant).RequireAuthorization();

        // HMS Meetings
        app.MapGet("/api/hms/meetings", ListMeetings).RequireAuthorization();
        app.MapPost("/api/hms/meetings", CreateMeeting).RequireAuthorization();
        app.MapGet("/api/hms/meetings/{id:guid}", GetMeetingDetail).RequireAuthorization();
        app.MapPost("/api/hms/meetings/{id:guid}/action-items", AddActionItem).RequireAuthorization();
        app.MapPut("/api/hms/meetings/{id:guid}/minutes", UpdateMinutes).RequireAuthorization();

        // Safety Round Schedules
        app.MapGet("/api/hms/safety-round-schedules", ListSafetyRoundSchedules).RequireAuthorization();

        return app;
    }

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static async Task<IResult> ListSjaForms(
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var forms = await db.SjaForms
            .Where(f => f.TenantId == tenantProvider.TenantId.Value)
            .OrderByDescending(f => f.Date)
            .Select(f => new SjaFormListItemDto(
                f.Id,
                f.Title,
                f.Status,
                f.ProjectId != null
                    ? db.Projects.Where(p => p.Id == f.ProjectId).Select(p => p.Name).FirstOrDefault()
                    : null,
                f.Date,
                db.SjaParticipants.Count(p => p.SjaFormId == f.Id)))
            .ToListAsync(ct);

        return Results.Ok(forms);
    }

    private static async Task<IResult> CreateSjaForm(
        CreateSjaFormRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "Tittel er påkrevd." });

        var personId = GetPersonId(user);
        if (personId is null || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var form = new SjaForm
        {
            TenantId = tenantProvider.TenantId.Value,
            Title = request.Title,
            Description = request.Description,
            ProjectId = request.ProjectId,
            Date = request.Date,
            Location = request.Location,
            Status = "Draft",
            CreatedById = personId.Value
        };

        db.SjaForms.Add(form);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hms/sja/{form.Id}", new { id = form.Id });
    }

    private static async Task<IResult> GetSjaFormDetail(
        Guid id,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var form = await db.SjaForms
            .Where(f => f.Id == id && f.TenantId == tenantProvider.TenantId.Value)
            .Select(f => new SjaFormDetailDto(
                f.Id,
                f.Title,
                f.Description,
                f.Status,
                f.ProjectId != null
                    ? db.Projects.Where(p => p.Id == f.ProjectId).Select(p => p.Name).FirstOrDefault()
                    : null,
                f.Date,
                f.Location,
                db.SjaHazards
                    .Where(h => h.SjaFormId == f.Id)
                    .OrderBy(h => h.SortOrder)
                    .Select(h => new SjaHazardDto(
                        h.Id,
                        h.Description,
                        h.Probability,
                        h.Consequence,
                        h.RiskScore,
                        h.Mitigation))
                    .ToList(),
                db.SjaParticipants
                    .Where(p => p.SjaFormId == f.Id)
                    .Select(p => new SjaParticipantDto(
                        p.Id,
                        p.IsExternal ? (p.ExternalName ?? "Ukjent") : (db.Persons.Where(per => per.Id == p.PersonId).Select(per => per.FullName).FirstOrDefault() ?? ""),
                        p.SignedAt != null,
                        p.IsExternal,
                        p.ExternalPhone,
                        p.ExternalCompany))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return form is not null ? Results.Ok(form) : Results.NotFound();
    }

    private static async Task<IResult> AddHazard(
        Guid id,
        AddSjaHazardRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest(new { error = "Beskrivelse er påkrevd." });

        var exists = await db.SjaForms.AnyAsync(f => f.Id == id && f.TenantId == tenantProvider.TenantId.Value, ct);
        if (!exists)
            return Results.NotFound();

        var nextSort = await db.SjaHazards
            .Where(h => h.SjaFormId == id)
            .CountAsync(ct);

        var hazard = new SjaHazard
        {
            SjaFormId = id,
            Description = request.Description,
            Probability = request.Probability,
            Consequence = request.Consequence,
            RiskScore = request.Probability * request.Consequence,
            Mitigation = request.Mitigation,
            SortOrder = nextSort
        };

        db.SjaHazards.Add(hazard);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hms/sja/{id}/hazards/{hazard.Id}", new { id = hazard.Id, riskScore = hazard.RiskScore });
    }

    private static async Task<IResult> AddParticipant(
        Guid id,
        AddSjaParticipantRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var exists = await db.SjaForms.AnyAsync(f => f.Id == id && f.TenantId == tenantProvider.TenantId.Value, ct);
        if (!exists)
            return Results.NotFound();

        var participant = new SjaParticipant
        {
            SjaFormId = id,
            PersonId = request.PersonId
        };

        db.SjaParticipants.Add(participant);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hms/sja/{id}/participants/{participant.Id}", new { id = participant.Id });
    }

    private static async Task<IResult> AddExternalParticipant(
        Guid id,
        AddExternalParticipantRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(request.Name)) return Results.BadRequest(new { error = "Navn er pakrevd." });

        var exists = await db.SjaForms.AnyAsync(f => f.Id == id && f.TenantId == tenantProvider.TenantId.Value, ct);
        if (!exists) return Results.NotFound();

        var participant = new SjaParticipant
        {
            SjaFormId = id,
            IsExternal = true,
            ExternalName = request.Name.Trim(),
            ExternalPhone = request.Phone?.Trim(),
            ExternalCompany = request.Company?.Trim()
        };

        db.SjaParticipants.Add(participant);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/hms/sja/{id}/participants/{participant.Id}", new { id = participant.Id });
    }

    private static async Task<IResult> RemoveParticipant(
        Guid id,
        Guid participantId,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        var participant = await db.SjaParticipants.FirstOrDefaultAsync(p => p.Id == participantId && p.SjaFormId == id, ct);
        if (participant is null) return Results.NotFound();

        db.SjaParticipants.Remove(participant);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ListMeetings(
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var meetings = await db.HmsMeetings
            .Where(m => m.TenantId == tenantProvider.TenantId.Value)
            .OrderByDescending(m => m.Date)
            .Select(m => new HmsMeetingListItemDto(
                m.Id,
                m.Title,
                m.Date,
                m.Location))
            .ToListAsync(ct);

        return Results.Ok(meetings);
    }

    private static async Task<IResult> CreateMeeting(
        CreateHmsMeetingRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Results.BadRequest(new { error = "Tittel er påkrevd." });

        var personId = GetPersonId(user);
        if (personId is null || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var meeting = new HmsMeeting
        {
            TenantId = tenantProvider.TenantId.Value,
            Title = request.Title,
            Date = request.Date,
            Location = request.Location,
            CreatedById = personId.Value
        };

        db.HmsMeetings.Add(meeting);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hms/meetings/{meeting.Id}", new { id = meeting.Id });
    }

    private static async Task<IResult> GetMeetingDetail(
        Guid id,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var meeting = await db.HmsMeetings
            .Where(m => m.Id == id && m.TenantId == tenantProvider.TenantId.Value)
            .Select(m => new HmsMeetingDetailDto(
                m.Id,
                m.Title,
                m.Date,
                m.Location,
                null, // Agenda stored in minutes for now
                db.HmsMeetingMinutes
                    .Where(min => min.MeetingId == m.Id)
                    .OrderByDescending(min => min.CreatedAt)
                    .Select(min => min.Content)
                    .FirstOrDefault(),
                db.HmsMeetingActionItems
                    .Where(ai => ai.MeetingId == m.Id)
                    .OrderBy(ai => ai.CreatedAt)
                    .Select(ai => new HmsActionItemDto(
                        ai.Id,
                        ai.Description,
                        ai.AssignedToId != null
                            ? db.Persons.Where(p => p.Id == ai.AssignedToId).Select(p => p.FullName).FirstOrDefault()
                            : null,
                        ai.Deadline,
                        ai.IsCompleted ? "Fullført" : "Åpen"))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return meeting is not null ? Results.Ok(meeting) : Results.NotFound();
    }

    private static async Task<IResult> AddActionItem(
        Guid id,
        CreateActionItemRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Description))
            return Results.BadRequest(new { error = "Beskrivelse er påkrevd." });

        var exists = await db.HmsMeetings.AnyAsync(m => m.Id == id && m.TenantId == tenantProvider.TenantId.Value, ct);
        if (!exists)
            return Results.NotFound();

        var item = new HmsMeetingActionItem
        {
            MeetingId = id,
            Description = request.Description,
            AssignedToId = request.ResponsibleId,
            Deadline = request.Deadline
        };

        db.HmsMeetingActionItems.Add(item);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/hms/meetings/{id}/action-items/{item.Id}", new { id = item.Id });
    }

    private static async Task<IResult> UpdateMinutes(
        Guid id,
        UpdateMinutesRequest request,
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var exists = await db.HmsMeetings.AnyAsync(m => m.Id == id && m.TenantId == tenantProvider.TenantId.Value, ct);
        if (!exists)
            return Results.NotFound();

        var existing = await db.HmsMeetingMinutes
            .Where(m => m.MeetingId == id)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            existing.Content = request.Minutes;
        }
        else
        {
            db.HmsMeetingMinutes.Add(new HmsMeetingMinutes
            {
                MeetingId = id,
                Content = request.Minutes
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ListSafetyRoundSchedules(
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var schedules = await db.SafetyRoundSchedules
            .Where(s => s.TenantId == tenantProvider.TenantId.Value && s.IsActive)
            .OrderBy(s => s.NextDue)
            .Select(s => new SafetyRoundScheduleDto(
                s.Id,
                s.Name,
                s.ProjectId,
                s.FrequencyWeeks,
                s.NextDue,
                s.IsActive))
            .ToListAsync(ct);

        return Results.Ok(schedules);
    }
}
