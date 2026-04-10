using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Notifications;

public class Announcement : TenantScopedEntity
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int UrgencyLevel { get; set; } = 1;
    public Guid CreatedById { get; set; }
    public bool RequiresAcknowledgment { get; set; }

    // Navigation
    public ICollection<AnnouncementAcknowledgment> Acknowledgments { get; set; } = [];
}
