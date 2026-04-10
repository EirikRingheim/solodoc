using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Contacts;

public class ContactProjectLink : BaseEntity
{
    public Guid ContactId { get; set; }
    public Guid ProjectId { get; set; }
    public string? Role { get; set; }

    // Navigation
    public Contact Contact { get; set; } = null!;
}
