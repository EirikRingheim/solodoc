using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Calendar;

public class EventInvitation : BaseEntity
{
    public Guid EventId { get; set; }
    public Guid PersonId { get; set; }
    public EventInvitationStatus Status { get; set; } = EventInvitationStatus.Pending;

    // Navigation
    public CalendarEvent Event { get; set; } = null!;
}
