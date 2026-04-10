namespace Solodoc.Shared.Calendar;

public record CalendarEventDto(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    bool IsAllDay,
    string? Location);

public record CrossTenantCalendarEventDto(
    Guid Id,
    Guid TenantId,
    string TenantName,
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    bool IsAllDay,
    string? Location);

public record CreateCalendarEventRequest(
    string Title,
    string? Description,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    bool IsAllDay,
    string? Location);
