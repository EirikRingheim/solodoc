using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class AllowanceGroup : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // Navigation
    public ICollection<AllowanceGroupMember> Members { get; set; } = [];
    public ICollection<AllowanceGroupRule> Rules { get; set; } = [];
}
