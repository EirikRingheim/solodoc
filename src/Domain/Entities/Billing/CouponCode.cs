using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Billing;

public class CouponCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TrialDays { get; set; } = 365;
    public int MaxRedemptions { get; set; } // 0 = unlimited
    public int TimesRedeemed { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? ExpiresAt { get; set; }

    public ICollection<CouponRedemption> Redemptions { get; set; } = [];
}

public class CouponRedemption : BaseEntity
{
    public Guid CouponCodeId { get; set; }
    public Guid TenantId { get; set; }
    public Guid RedeemedById { get; set; }
    public DateTimeOffset RedeemedAt { get; set; } = DateTimeOffset.UtcNow;

    public CouponCode CouponCode { get; set; } = null!;
}
