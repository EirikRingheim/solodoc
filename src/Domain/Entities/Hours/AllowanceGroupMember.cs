using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class AllowanceGroupMember : BaseEntity
{
    public Guid AllowanceGroupId { get; set; }
    public Guid PersonId { get; set; }

    // Navigation
    public AllowanceGroup AllowanceGroup { get; set; } = null!;
}
