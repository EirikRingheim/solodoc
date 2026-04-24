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

    // Onboarding
    public bool OnboardingCompleted { get; set; }
    public string? IndustryType { get; set; } // Bygg, Elektro, Rorlegger, Snekker, Landbruk, Maskin, Annet
    public string? CompanySize { get; set; } // 1-5, 6-15, 16-50, 50+
    public string? EnabledModules { get; set; } // JSON array of enabled module keys

    // Pay period
    public int DefaultPayPeriodStartDay { get; set; } = 15; // day of month (15 = 15th–14th, typical for payroll)

    // Feature flags — granular control over what's enabled
    public string? FeatureFlags { get; set; } // JSON: { "hours.requireApproval": true, ... }

    // Subscription
    public string SubscriptionTier { get; set; } = "trial"; // trial, basic, pro, enterprise
    public DateTimeOffset? TrialStartedAt { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset? SubscriptionStartedAt { get; set; }
    public int? MaxUsers { get; set; }

    // Navigation properties
    public ICollection<TenantMembership> Memberships { get; set; } = [];
    public ICollection<Invitation> Invitations { get; set; } = [];
}
