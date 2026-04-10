using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Auth;

public class CustomRole : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Color { get; set; }

    /// <summary>
    /// JSON array of permission keys, e.g. ["projects.view","projects.create","hours.register"]
    /// </summary>
    public string Permissions { get; set; } = "[]";

    /// <summary>
    /// Whether this is a system-default role that can't be deleted (Admin, Prosjektleder, Feltarbeider)
    /// </summary>
    public bool IsSystem { get; set; }
}
