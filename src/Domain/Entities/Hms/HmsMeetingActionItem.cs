using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hms;

public class HmsMeetingActionItem : BaseEntity
{
    public Guid MeetingId { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid? AssignedToId { get; set; }
    public DateOnly? Deadline { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation
    public HmsMeeting Meeting { get; set; } = null!;
}
