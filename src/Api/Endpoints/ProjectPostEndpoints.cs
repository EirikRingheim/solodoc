using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Projects;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Notifications;

namespace Solodoc.Api.Endpoints;

public static class ProjectPostEndpoints
{
    public static WebApplication MapProjectPostEndpoints(this WebApplication app)
    {
        app.MapGet("/api/projects/{projectId:guid}/posts", GetPosts).RequireAuthorization();
        app.MapPost("/api/projects/{projectId:guid}/posts", CreatePost).RequireAuthorization();
        app.MapDelete("/api/projects/{projectId:guid}/posts/{postId:guid}", DeletePost).RequireAuthorization();
        app.MapPost("/api/projects/{projectId:guid}/posts/{postId:guid}/comments", AddComment).RequireAuthorization();
        app.MapPatch("/api/projects/{projectId:guid}/posts/{postId:guid}/pin", TogglePin).RequireAuthorization();

        return app;
    }

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var c = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(c, out var pid) ? pid : null;
    }

    private static async Task<IResult> GetPosts(
        Guid projectId,
        SolodocDbContext db,
        ITenantProvider tp,
        IFileStorageService fileStorage,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var posts = await db.ProjectPosts
            .Where(p => p.ProjectId == projectId && !p.IsDeleted)
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.CreatedAt)
            .Include(p => p.Comments.Where(c => !c.IsDeleted))
            .Take(50)
            .ToListAsync(ct);

        var personIds = posts.Select(p => p.AuthorId)
            .Concat(posts.SelectMany(p => p.Comments.Select(c => c.AuthorId)))
            .Distinct().ToList();

        var names = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var result = new List<ProjectPostDto>();
        foreach (var p in posts)
        {
            names.TryGetValue(p.AuthorId, out var authorName);
            var initials = GetInitials(authorName ?? "?");

            string? photoUrl = null;
            if (!string.IsNullOrEmpty(p.PhotoFileKey))
            {
                try { photoUrl = await fileStorage.GetPresignedUrlAsync(p.PhotoFileKey, TimeSpan.FromHours(1), ct); }
                catch { }
            }

            var comments = p.Comments.OrderBy(c => c.CreatedAt).Select(c =>
            {
                names.TryGetValue(c.AuthorId, out var cName);
                return new ProjectPostCommentDto(c.Id, cName ?? "", c.Content, c.CreatedAt);
            }).ToList();

            result.Add(new ProjectPostDto(
                p.Id, p.ProjectId, authorName ?? "", initials,
                p.Content, p.PhotoFileKey, photoUrl,
                p.IsPinned, p.CreatedAt, comments));
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> CreatePost(
        Guid projectId,
        CreateProjectPostRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var pid = GetPersonId(user);
        if (pid is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest(new { error = "Innhold er påkrevd." });

        var post = new ProjectPost
        {
            ProjectId = projectId,
            AuthorId = pid.Value,
            Content = request.Content.Trim(),
            PhotoFileKey = request.PhotoFileKey
        };

        db.ProjectPosts.Add(post);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/projects/{projectId}/posts/{post.Id}", new { id = post.Id });
    }

    private static async Task<IResult> DeletePost(
        Guid projectId, Guid postId,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var post = await db.ProjectPosts.FirstOrDefaultAsync(
            p => p.Id == postId && p.ProjectId == projectId && !p.IsDeleted, ct);
        if (post is null) return Results.NotFound();

        post.IsDeleted = true;
        post.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> AddComment(
        Guid projectId, Guid postId,
        CreateProjectPostCommentRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var pid = GetPersonId(user);
        if (pid is null) return Results.Unauthorized();

        var post = await db.ProjectPosts.FirstOrDefaultAsync(
            p => p.Id == postId && p.ProjectId == projectId && !p.IsDeleted, ct);
        if (post is null) return Results.NotFound();

        var comment = new ProjectPostComment
        {
            PostId = postId,
            AuthorId = pid.Value,
            Content = request.Content.Trim()
        };

        db.ProjectPostComments.Add(comment);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = comment.Id });
    }

    private static async Task<IResult> TogglePin(
        Guid projectId, Guid postId,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var post = await db.ProjectPosts.FirstOrDefaultAsync(
            p => p.Id == postId && p.ProjectId == projectId && !p.IsDeleted, ct);
        if (post is null) return Results.NotFound();

        post.IsPinned = !post.IsPinned;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { pinned = post.IsPinned });
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..1].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
        };
    }
}
