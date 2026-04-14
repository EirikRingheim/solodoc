using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Notifications;

public class AnnouncementComment : BaseEntity
{
    public Guid AnnouncementId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;

    public Announcement Announcement { get; set; } = null!;
}
