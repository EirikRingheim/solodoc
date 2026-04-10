using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Deviations;

public class DeviationVisibility : BaseEntity
{
    public Guid DeviationId { get; set; }
    public Guid PersonId { get; set; }

    public Deviation Deviation { get; set; } = null!;
}
