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
    DateTimeOffset CreatedAt,
    bool RequiresAcknowledgment,
    bool HasAcknowledged);

public record CreateAnnouncementRequest(
    string Title,
    string Body,
    int UrgencyLevel,
    bool RequiresAcknowledgment);
