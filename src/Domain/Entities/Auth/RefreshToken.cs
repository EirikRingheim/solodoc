using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Auth;

public class RefreshToken : BaseEntity
{
    public Guid PersonId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    // Navigation properties
    public Person Person { get; set; } = null!;

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
}
