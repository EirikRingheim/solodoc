using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Notifications;

public class Notification : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string? LinkUrl { get; set; }
    public string? Type { get; set; }
}
