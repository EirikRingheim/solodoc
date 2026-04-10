using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hms;

public class HmsMeeting : TenantScopedEntity
{
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public Guid CreatedById { get; set; }

    // Navigation
    public ICollection<HmsMeetingMinutes> Minutes { get; set; } = [];
    public ICollection<HmsMeetingActionItem> ActionItems { get; set; } = [];
}
