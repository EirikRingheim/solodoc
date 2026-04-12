using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Billing;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Admin;

namespace Solodoc.Api.Endpoints;

public static class CouponEndpoints
{
    public static WebApplication MapCouponEndpoints(this WebApplication app)
    {
        app.MapPost("/api/coupons/redeem", RedeemCoupon).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> RedeemCoupon(
        RedeemCouponRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId)) return Results.Unauthorized();

        var code = request.Code.Trim().ToUpperInvariant();
        var coupon = await db.CouponCodes
            .FirstOrDefaultAsync(c => c.Code == code && c.IsActive && !c.IsDeleted, ct);

        if (coupon is null)
            return Results.Ok(new RedeemCouponResult(false, "Ugyldig kupongkode.", null));

        if (coupon.ExpiresAt.HasValue && coupon.ExpiresAt < DateTimeOffset.UtcNow)
            return Results.Ok(new RedeemCouponResult(false, "Kupongkoden har utlopt.", null));

        if (coupon.MaxRedemptions > 0 && coupon.TimesRedeemed >= coupon.MaxRedemptions)
            return Results.Ok(new RedeemCouponResult(false, "Kupongkoden er brukt opp.", null));

        // Check if already redeemed by this tenant
        var alreadyRedeemed = await db.CouponRedemptions
            .AnyAsync(r => r.CouponCodeId == coupon.Id && r.TenantId == tenantId && !r.IsDeleted, ct);
        if (alreadyRedeemed)
            return Results.Ok(new RedeemCouponResult(false, "Denne kupongkoden er allerede brukt.", null));

        // Apply coupon - extend trial
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant is null) return Results.NotFound();

        var baseDate = tenant.TrialEndsAt ?? DateTimeOffset.UtcNow;
        tenant.TrialEndsAt = baseDate.AddDays(coupon.TrialDays);
        tenant.SubscriptionTier = "trial";
        if (tenant.TrialStartedAt is null) tenant.TrialStartedAt = DateTimeOffset.UtcNow;

        // Record redemption
        db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponCodeId = coupon.Id,
            TenantId = tenantId,
            RedeemedById = personId
        });
        coupon.TimesRedeemed++;

        await db.SaveChangesAsync(ct);

        return Results.Ok(new RedeemCouponResult(true, null, coupon.TrialDays));
    }
}
