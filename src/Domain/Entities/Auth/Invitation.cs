using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Auth;

public class Invitation : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public InvitationType Type { get; set; }
    public TenantRole IntendedRole { get; set; }
    public Guid? ProjectId { get; set; }
    public InvitationState State { get; set; } = InvitationState.Pending;
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid InvitedBy { get; set; }
    public string InvitedByName { get; set; } = string.Empty;
    public DateTimeOffset? AcceptedAt { get; set; }
    public Guid? AcceptedByPersonId { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
}
