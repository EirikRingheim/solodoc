using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class RotationPattern : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;  // "Tunnelskift 12/16", "Offshore 14/14"
    public int CycleLengthDays { get; set; }           // Total days in one cycle

    // Navigation
    public ICollection<RotationPatternDay> Days { get; set; } = [];
}
