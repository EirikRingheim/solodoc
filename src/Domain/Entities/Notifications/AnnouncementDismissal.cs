namespace Solodoc.Domain.Entities.Notifications;

public class AnnouncementDismissal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AnnouncementId { get; set; }
    public Guid PersonId { get; set; }
    public DateTimeOffset DismissedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Announcement Announcement { get; set; } = null!;
}
