using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Notifications;

public class AnnouncementAcknowledgment : BaseEntity
{
    public Guid AnnouncementId { get; set; }
    public Guid PersonId { get; set; }
    public DateTimeOffset AcknowledgedAt { get; set; }

    // Navigation
    public Announcement Announcement { get; set; } = null!;
}
