using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Deviations;

public class RelatedDeviation : BaseEntity
{
    public Guid DeviationId { get; set; }
    public Guid RelatedDeviationId { get; set; }

    public Deviation Deviation { get; set; } = null!;
    public Deviation Related { get; set; } = null!;
}
