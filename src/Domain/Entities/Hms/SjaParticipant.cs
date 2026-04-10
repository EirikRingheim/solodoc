using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hms;

public class SjaParticipant : BaseEntity
{
    public Guid SjaFormId { get; set; }
    public Guid PersonId { get; set; }
    public string? SignatureFileKey { get; set; }
    public DateTimeOffset? SignedAt { get; set; }

    // Navigation
    public SjaForm SjaForm { get; set; } = null!;
}
