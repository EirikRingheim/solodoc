using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Notifications;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Notifications;

namespace Solodoc.Api.Endpoints;

public static class NotificationEndpoints
{
    public static WebApplication MapNotificationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/notifications", ListNotifications).RequireAuthorization();
        app.MapPatch("/api/notifications/{id:guid}/read", MarkAsRead).RequireAuthorization();
        app.MapPatch("/api/notifications/read-all", MarkAllAsRead).RequireAuthorization();
        app.MapGet("/api/announcements", ListAnnouncements).RequireAuthorization();
        app.MapPost("/api/announcements", CreateAnnouncement).RequireAuthorization();
        app.MapPost("/api/announcements/{id:guid}/acknowledge", Acknowledge).RequireAuthorization();

        return app;
    }

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? user.FindFirstValue("sub");
        return Guid.TryParse(personIdClaim, out var personId) ? personId : null;
    }

    private static async Task<IResult> ListNotifications(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var items = await db.Notifications
            .Where(n => n.PersonId == personId.Value)
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id,
                n.Title,
                n.Message,
                n.IsRead,
                n.CreatedAt,
                n.LinkUrl,
                n.Type))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> MarkAsRead(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.PersonId == personId.Value, ct);
        if (notification is null)
            return Results.NotFound();

        notification.IsRead = true;
        notification.ReadAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> MarkAllAsRead(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var unread = await db.Notifications
            .Where(n => n.PersonId == personId.Value && !n.IsRead)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ListAnnouncements(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var items = await db.Announcements
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementDto(
                a.Id,
                a.Title,
                a.Body,
                a.UrgencyLevel,
                db.Persons.Where(p => p.Id == a.CreatedById).Select(p => p.FullName).FirstOrDefault() ?? "",
                a.CreatedAt,
                a.RequiresAcknowledgment,
                a.Acknowledgments.Any(ack => ack.PersonId == personId.Value)))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> CreateAnnouncement(
        CreateAnnouncementRequest request,
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

        var announcement = new Announcement
        {
            TenantId = tenantProvider.TenantId.Value,
            Title = request.Title,
            Body = request.Body,
            UrgencyLevel = request.UrgencyLevel,
            CreatedById = personId.Value,
            RequiresAcknowledgment = request.RequiresAcknowledgment
        };

        db.Announcements.Add(announcement);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/announcements/{announcement.Id}", new { id = announcement.Id });
    }

    private static async Task<IResult> Acknowledge(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var announcement = await db.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (announcement is null)
            return Results.NotFound();

        var alreadyAcked = await db.AnnouncementAcknowledgments
            .AnyAsync(a => a.AnnouncementId == id && a.PersonId == personId.Value, ct);
        if (alreadyAcked)
            return Results.Ok();

        var ack = new AnnouncementAcknowledgment
        {
            AnnouncementId = id,
            PersonId = personId.Value,
            AcknowledgedAt = DateTimeOffset.UtcNow
        };

        db.AnnouncementAcknowledgments.Add(ack);
        await db.SaveChangesAsync(ct);

        return Results.Ok();
    }
}
