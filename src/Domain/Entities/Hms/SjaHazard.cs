using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hms;

public class SjaHazard : BaseEntity
{
    public Guid SjaFormId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Probability { get; set; }
    public int Consequence { get; set; }
    public int RiskScore { get; set; }
    public string? Mitigation { get; set; }
    public int SortOrder { get; set; }

    // Navigation
    public SjaForm SjaForm { get; set; } = null!;
}
