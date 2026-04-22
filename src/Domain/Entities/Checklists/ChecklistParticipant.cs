using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Checklists;

public class ChecklistParticipant : BaseEntity
{
    public Guid ChecklistInstanceId { get; set; }

    // Internal participant (employee or UE with account)
    public Guid? PersonId { get; set; }

    // External participant (UE without account)
    public bool IsExternal { get; set; }
    public string? ExternalName { get; set; }
    public string? ExternalPhone { get; set; }
    public string? ExternalCompany { get; set; }

    // Signature
    public string? SignatureFileKey { get; set; }
    public DateTimeOffset? SignedAt { get; set; }

    // Navigation
    public ChecklistInstance ChecklistInstance { get; set; } = null!;
}
