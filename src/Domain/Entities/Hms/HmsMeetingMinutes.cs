using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hms;

public class HmsMeetingMinutes : BaseEntity
{
    public Guid MeetingId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FileKey { get; set; }

    // Navigation
    public HmsMeeting Meeting { get; set; } = null!;
}
