namespace Solodoc.Shared.Notifications;

public record NotificationDto(
    Guid Id,
    string Title,
    string? Message,
    bool IsRead,
    DateTimeOffset CreatedAt,
    string? LinkUrl,
    string? Type);

public record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    int UrgencyLevel,
    string CreatedBy,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    bool RequiresAcknowledgment,
    bool HasAcknowledged,
    bool IsDismissed = false,
    string? PhotoFileKey = null,
    string? PhotoUrl = null,
    List<AnnouncementCommentDto>? Comments = null);

public record AnnouncementCommentDto(
    Guid Id,
    string AuthorName,
    string Content,
    DateTimeOffset CreatedAt);

public record CreateAnnouncementRequest(
    string Title,
    string Body,
    int UrgencyLevel,
    bool RequiresAcknowledgment,
    string? PhotoFileKey = null,
    List<Guid>? TargetPersonIds = null);

public record CreateAnnouncementCommentRequest(string Content);

// ── Project Posts (Innlegg) ──────────────────────────

public record ProjectPostDto(
    Guid Id,
    Guid ProjectId,
    string AuthorName,
    string AuthorInitials,
    string Content,
    string? PhotoFileKey,
    string? PhotoUrl,
    bool IsPinned,
    DateTimeOffset CreatedAt,
    List<ProjectPostCommentDto> Comments);

public record ProjectPostCommentDto(
    Guid Id,
    string AuthorName,
    string Content,
    DateTimeOffset CreatedAt);

public record CreateProjectPostRequest(
    string Content,
    string? PhotoFileKey);

public record CreateProjectPostCommentRequest(
    string Content);
