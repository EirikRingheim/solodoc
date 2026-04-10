using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Projects;

public class Customer : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public CustomerType Type { get; set; }
    public string? OrgNumber { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? ContactPersonName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
}
