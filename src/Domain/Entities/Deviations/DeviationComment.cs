using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Deviations;

public class DeviationComment : BaseEntity
{
    public Guid DeviationId { get; set; }
    public Guid AuthorId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset PostedAt { get; set; } = DateTimeOffset.UtcNow;

    public Deviation Deviation { get; set; } = null!;
}
