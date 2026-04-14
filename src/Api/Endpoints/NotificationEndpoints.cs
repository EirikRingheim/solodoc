using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
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
        app.MapPut("/api/announcements/{id:guid}", UpdateAnnouncement).RequireAuthorization();
        app.MapDelete("/api/announcements/{id:guid}", DeleteAnnouncement).RequireAuthorization();
        app.MapPost("/api/announcements/{id:guid}/comments", AddAnnouncementComment).RequireAuthorization();

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
        ITenantProvider tenantProvider,
        IFileStorageService fileStorage,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var pidStr = personId.Value.ToString();
        var announcements = await db.Announcements
            .Where(a => a.TenantId == tenantProvider.TenantId.Value)
            .Where(a => a.TargetPersonIds == null || a.TargetPersonIds.Contains(pidStr))
            .OrderByDescending(a => a.CreatedAt)
            .Include(a => a.Comments.Where(c => !c.IsDeleted))
            .Include(a => a.Acknowledgments)
            .Take(20)
            .ToListAsync(ct);

        var personIds = announcements.Select(a => a.CreatedById)
            .Concat(announcements.SelectMany(a => a.Comments.Select(c => c.AuthorId)))
            .Distinct().ToList();
        var names = await db.Persons.Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var items = new List<AnnouncementDto>();
        foreach (var a in announcements)
        {
            names.TryGetValue(a.CreatedById, out var creatorName);

            string? photoUrl = null;
            if (!string.IsNullOrEmpty(a.PhotoFileKey))
            {
                try { photoUrl = await fileStorage.GetPresignedUrlAsync(a.PhotoFileKey, TimeSpan.FromHours(1), ct); }
                catch { }
            }

            var comments = a.Comments.OrderBy(c => c.CreatedAt).Select(c =>
            {
                names.TryGetValue(c.AuthorId, out var cName);
                return new AnnouncementCommentDto(c.Id, cName ?? "", c.Content, c.CreatedAt);
            }).ToList();

            items.Add(new AnnouncementDto(
                a.Id, a.Title, a.Body, a.UrgencyLevel,
                creatorName ?? "", a.CreatedById, a.CreatedAt,
                a.RequiresAcknowledgment,
                a.Acknowledgments.Any(ack => ack.PersonId == personId.Value),
                a.PhotoFileKey, photoUrl, comments));
        }

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
            RequiresAcknowledgment = request.RequiresAcknowledgment,
            PhotoFileKey = request.PhotoFileKey,
            TargetPersonIds = request.TargetPersonIds is { Count: > 0 }
                ? System.Text.Json.JsonSerializer.Serialize(request.TargetPersonIds)
                : null
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

    // ── Update announcement (admin only, own announcements) ──

    private static async Task<IResult> UpdateAnnouncement(
        Guid id,
        CreateAnnouncementRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null || tp.TenantId is null) return Results.Unauthorized();

        var ann = await db.Announcements.FirstOrDefaultAsync(
            a => a.Id == id && a.TenantId == tp.TenantId.Value, ct);
        if (ann is null) return Results.NotFound();

        ann.Title = request.Title;
        ann.Body = request.Body;
        ann.UrgencyLevel = request.UrgencyLevel;
        ann.RequiresAcknowledgment = request.RequiresAcknowledgment;
        if (request.PhotoFileKey is not null) ann.PhotoFileKey = request.PhotoFileKey;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Delete announcement ──

    private static async Task<IResult> DeleteAnnouncement(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null || tp.TenantId is null) return Results.Unauthorized();

        var ann = await db.Announcements.FirstOrDefaultAsync(
            a => a.Id == id && a.TenantId == tp.TenantId.Value, ct);
        if (ann is null) return Results.NotFound();

        ann.IsDeleted = true;
        ann.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Add comment to announcement ──

    private static async Task<IResult> AddAnnouncementComment(
        Guid id,
        CreateAnnouncementCommentRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null || tp.TenantId is null) return Results.Unauthorized();

        var ann = await db.Announcements.FirstOrDefaultAsync(
            a => a.Id == id && a.TenantId == tp.TenantId.Value, ct);
        if (ann is null) return Results.NotFound();

        db.AnnouncementComments.Add(new AnnouncementComment
        {
            AnnouncementId = id,
            AuthorId = personId.Value,
            Content = request.Content.Trim()
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
