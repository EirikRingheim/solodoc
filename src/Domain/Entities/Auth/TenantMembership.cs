using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Auth;

public class TenantMembership : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid TenantId { get; set; }
    public TenantRole Role { get; set; }
    public Guid? CustomRoleId { get; set; }
    public TenantMembershipState State { get; set; } = TenantMembershipState.Active;
    public bool GpsConsent { get; set; }
    public DateTimeOffset? GpsConsentChangedAt { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public Guid? SuspendedBy { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
    public Guid? RemovedBy { get; set; }

    // Navigation properties
    public Person Person { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
