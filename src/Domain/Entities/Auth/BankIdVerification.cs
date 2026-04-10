using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Auth;

public class BankIdVerification : BaseEntity
{
    public Guid PersonId { get; set; }
    public string PersonalIdHash { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string OrgNumber { get; set; } = string.Empty;
    public DateTimeOffset VerifiedAt { get; set; }
    public string VerificationType { get; set; } = string.Empty; // TenantRegistration, OwnershipTransfer

    // Navigation properties
    public Person Person { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
