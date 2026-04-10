using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Calendar;

public class CalendarEvent : TenantScopedEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    public Guid CreatedById { get; set; }

    // Navigation
    public ICollection<EventInvitation> Invitations { get; set; } = [];
}
