using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Deviations;

public class DeviationCategory : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
