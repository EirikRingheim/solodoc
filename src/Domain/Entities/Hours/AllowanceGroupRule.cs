using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class AllowanceGroupRule : BaseEntity
{
    public Guid AllowanceGroupId { get; set; }
    public Guid AllowanceRuleId { get; set; }

    // Navigation
    public AllowanceGroup AllowanceGroup { get; set; } = null!;
    public AllowanceRule AllowanceRule { get; set; } = null!;
}
