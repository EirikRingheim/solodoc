using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Auth;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string OrgNumber { get; set; } = string.Empty;
    public BusinessType BusinessType { get; set; }
    public string? BusinessAddress { get; set; }
    public string? LogoFileKey { get; set; }
    public string? AccentColor { get; set; }
    public string? DefaultTimeZoneId { get; set; } = "Europe/Oslo";
    public bool GpsEnabled { get; set; }
    public bool IsFrozen { get; set; }
    public DateTimeOffset? FrozenAt { get; set; }

    // Hours settings
    public bool TimebankEnabled { get; set; } = true;
    public bool OvertimeStackingMode { get; set; } // false = highest wins, true = rates stack

    // Navigation properties
    public ICollection<TenantMembership> Memberships { get; set; } = [];
    public ICollection<Invitation> Invitations { get; set; } = [];
}
