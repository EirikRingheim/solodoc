using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Auth;

public class PasskeyCredential : BaseEntity
{
    public Guid PersonId { get; set; }
    public byte[] CredentialId { get; set; } = [];
    public byte[] PublicKey { get; set; } = [];
    public uint SignCount { get; set; }
    public string? DeviceName { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    // Navigation properties
    public Person Person { get; set; } = null!;
}
