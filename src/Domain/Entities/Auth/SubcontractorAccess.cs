using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Auth;

public class SubcontractorAccess : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public SubcontractorAccessState State { get; set; } = SubcontractorAccessState.Active;
    public bool HoursRegistrationEnabled { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }

    // Navigation properties
    public Person Person { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
