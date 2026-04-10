using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Contacts;

public class Contact : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public ContactType Type { get; set; }
    public string? OrgNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<ContactProjectLink> ProjectLinks { get; set; } = [];
}
