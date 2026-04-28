using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Auth;

public class Person : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public PersonState State { get; set; } = PersonState.Unverified;
    public SystemRole? SystemRole { get; set; }
    public string? TimeZoneId { get; set; }
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? PasswordResetToken { get; set; }
    public DateTimeOffset? PasswordResetExpiry { get; set; }
    public long? PowerOfficeEmployeeId { get; set; }

    // Navigation properties
    public ICollection<TenantMembership> TenantMemberships { get; set; } = [];
    public ICollection<SubcontractorAccess> SubcontractorAccesses { get; set; } = [];
    public ICollection<PasskeyCredential> PasskeyCredentials { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<BankIdVerification> BankIdVerifications { get; set; } = [];
}
