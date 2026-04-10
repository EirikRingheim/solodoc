using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Auth;

public class WorksiteCheckIn : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid TenantId { get; set; }

    // Where — one of these is set
    public Guid? ProjectId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? LocationId { get; set; }

    // When
    public DateTimeOffset CheckedInAt { get; set; }
    public DateTimeOffset? CheckedOutAt { get; set; }

    // How
    public string Source { get; set; } = "Manual"; // "Manual", "QrCode", "TimeClock"
    public bool AutoCheckedOut { get; set; }

    // GPS
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? LocationAccuracy { get; set; }
    public double? LatitudeOut { get; set; }
    public double? LongitudeOut { get; set; }

    // Navigation properties
    public Person Person { get; set; } = null!;
    public Tenant Tenant { get; set; } = null!;
}
