using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Domain.Entities.Billing;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Admin;
using Solodoc.Application.Common;
using Solodoc.Shared.Onboarding;

namespace Solodoc.Api.Endpoints;

public static class SuperAdminEndpoints
{
    public static WebApplication MapSuperAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization();

        // Tenant overview
        group.MapGet("/tenants", ListAllTenants);
        group.MapGet("/tenants/{id:guid}", GetTenantDetail);
        group.MapPost("/tenants/{id:guid}/freeze", FreezeTenant);
        group.MapPost("/tenants/{id:guid}/unfreeze", UnfreezeTenant);

        // Coupons
        group.MapGet("/coupons", ListCoupons);
        group.MapPost("/coupons", CreateCoupon);
        group.MapDelete("/coupons/{id:guid}", DeleteCoupon);

        // Invoices
        group.MapGet("/invoices", ListInvoices);
        group.MapPost("/invoices/generate", GenerateMonthlyInvoices);
        group.MapPut("/invoices/{id:guid}/status", UpdateInvoiceStatus);
        group.MapGet("/invoices/{id:guid}/ehf", DownloadEhfXml);

        // Client errors
        group.MapGet("/errors", ListClientErrors);
        group.MapPost("/errors/{id:guid}/resolve", ResolveClientError);

        // Public error reporting (any authenticated user)
        app.MapPost("/api/errors/report", ReportClientError).RequireAuthorization();

        return app;
    }

    // ── Helpers ──────────────────────────────────────

    private static bool IsSuperAdmin(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        return roles.Contains("SuperAdmin") || roles.Contains("super-admin");
    }

    // ── Tenant overview ─────────────────────────────

    private static async Task<IResult> ListAllTenants(
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var tenants = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var tenantIds = tenants.Select(t => t.Id).ToList();

        // Batch load stats
        var membershipStats = await db.TenantMemberships
            .IgnoreQueryFilters()
            .Where(m => tenantIds.Contains(m.TenantId) && m.State == TenantMembershipState.Active && !m.IsDeleted)
            .GroupBy(m => new { m.TenantId, m.Role })
            .Select(g => new { g.Key.TenantId, g.Key.Role, Count = g.Count() })
            .ToListAsync(ct);

        var projectCounts = await db.Projects
            .IgnoreQueryFilters()
            .Where(p => tenantIds.Contains(p.TenantId) && !p.IsDeleted)
            .GroupBy(p => p.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var jobCounts = await db.Jobs
            .IgnoreQueryFilters()
            .Where(j => tenantIds.Contains(j.TenantId) && !j.IsDeleted)
            .GroupBy(j => j.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var templateCounts = await db.ChecklistTemplates
            .IgnoreQueryFilters()
            .Where(t => tenantIds.Contains(t.TenantId) && !t.IsDeleted)
            .GroupBy(t => t.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var instanceCounts = await db.ChecklistInstances
            .IgnoreQueryFilters()
            .Where(i => tenantIds.Contains(i.TenantId) && !i.IsDeleted)
            .GroupBy(i => i.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var deviationStats = await db.Deviations
            .IgnoreQueryFilters()
            .Where(d => tenantIds.Contains(d.TenantId) && !d.IsDeleted)
            .GroupBy(d => d.TenantId)
            .Select(g => new { TenantId = g.Key, Total = g.Count(), Open = g.Count(d => d.Status == DeviationStatus.Open) })
            .ToListAsync(ct);

        var hourStats = await db.TimeEntries
            .IgnoreQueryFilters()
            .Where(te => tenantIds.Contains(te.TenantId) && !te.IsDeleted)
            .GroupBy(te => te.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count(), TotalHours = g.Sum(te => te.Hours) })
            .ToListAsync(ct);

        var chemicalCounts = await db.Chemicals
            .IgnoreQueryFilters()
            .Where(c => tenantIds.Contains(c.TenantId) && !c.IsDeleted)
            .GroupBy(c => c.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var equipmentCounts = await db.Equipment
            .IgnoreQueryFilters()
            .Where(e => tenantIds.Contains(e.TenantId) && !e.IsDeleted)
            .GroupBy(e => e.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var purchaseCounts = await db.MarketplacePurchases
            .IgnoreQueryFilters()
            .Where(p => tenantIds.Contains(p.TenantId) && !p.IsDeleted)
            .GroupBy(p => p.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Last activity (most recent time entry)
        var lastActivity = await db.TimeEntries
            .IgnoreQueryFilters()
            .Where(te => tenantIds.Contains(te.TenantId) && !te.IsDeleted)
            .GroupBy(te => te.TenantId)
            .Select(g => new { TenantId = g.Key, LastAt = g.Max(te => te.CreatedAt) })
            .ToListAsync(ct);

        // Coupon info
        var couponInfo = await db.CouponRedemptions
            .IgnoreQueryFilters()
            .Where(r => tenantIds.Contains(r.TenantId) && !r.IsDeleted)
            .Include(r => r.CouponCode)
            .GroupBy(r => r.TenantId)
            .Select(g => new { TenantId = g.Key, Code = g.OrderByDescending(r => r.RedeemedAt).First().CouponCode.Code, Days = g.OrderByDescending(r => r.RedeemedAt).First().CouponCode.TrialDays })
            .ToListAsync(ct);

        var result = tenants.Select(t =>
        {
            var ms = membershipStats.Where(m => m.TenantId == t.Id).ToList();
            var admins = ms.Where(m => m.Role == TenantRole.TenantAdmin).Sum(m => m.Count);
            var pls = ms.Where(m => m.Role == TenantRole.ProjectLeader).Sum(m => m.Count);
            var fws = ms.Where(m => m.Role == TenantRole.FieldWorker).Sum(m => m.Count);
            var total = admins + pls + fws;
            var subs = 0; // subcontractors tracked separately

            var monthlyKr = PricingConfig.CalculateMonthly(admins, pls + fws, subs);
            var coupon = couponInfo.FirstOrDefault(c => c.TenantId == t.Id);

            return new TenantOverviewDto(
                t.Id, t.Name, t.OrgNumber, t.BusinessAddress, t.AccentColor,
                t.IndustryType, t.CompanySize, t.SubscriptionTier,
                t.TrialStartedAt, t.TrialEndsAt, t.SubscriptionStartedAt,
                t.OnboardingCompleted, t.IsFrozen, t.CreatedAt,
                total, admins, pls, fws, subs,
                projectCounts.FirstOrDefault(p => p.TenantId == t.Id)?.Count ?? 0,
                jobCounts.FirstOrDefault(j => j.TenantId == t.Id)?.Count ?? 0,
                templateCounts.FirstOrDefault(tc => tc.TenantId == t.Id)?.Count ?? 0,
                instanceCounts.FirstOrDefault(ic => ic.TenantId == t.Id)?.Count ?? 0,
                deviationStats.FirstOrDefault(d => d.TenantId == t.Id)?.Total ?? 0,
                deviationStats.FirstOrDefault(d => d.TenantId == t.Id)?.Open ?? 0,
                hourStats.FirstOrDefault(h => h.TenantId == t.Id)?.Count ?? 0,
                (decimal)(hourStats.FirstOrDefault(h => h.TenantId == t.Id)?.TotalHours ?? 0),
                chemicalCounts.FirstOrDefault(c => c.TenantId == t.Id)?.Count ?? 0,
                equipmentCounts.FirstOrDefault(e => e.TenantId == t.Id)?.Count ?? 0,
                purchaseCounts.FirstOrDefault(p => p.TenantId == t.Id)?.Count ?? 0,
                lastActivity.FirstOrDefault(la => la.TenantId == t.Id)?.LastAt,
                monthlyKr,
                coupon is not null,
                coupon?.Code,
                coupon?.Days);
        }).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> GetTenantDetail(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);
        if (tenant is null) return Results.NotFound();

        // Get employees
        var employees = await db.TenantMemberships
            .IgnoreQueryFilters()
            .Where(m => m.TenantId == id && m.State == TenantMembershipState.Active && !m.IsDeleted)
            .Include(m => m.Person)
            .ToListAsync(ct);

        var now = DateTimeOffset.UtcNow;
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var personIds = employees.Select(e => e.PersonId).ToList();
        var hoursThisMonth = await db.TimeEntries
            .IgnoreQueryFilters()
            .Where(te => te.TenantId == id && !te.IsDeleted && personIds.Contains(te.PersonId) && te.Date >= DateOnly.FromDateTime(monthStart.DateTime))
            .GroupBy(te => te.PersonId)
            .Select(g => new { PersonId = g.Key, Hours = g.Sum(te => te.Hours) })
            .ToListAsync(ct);

        var employeeDtos = employees.Select(e => new TenantEmployeeDto(
            e.PersonId, e.Person.FullName, e.Person.Email,
            e.Role == TenantRole.TenantAdmin ? "Admin" : e.Role == TenantRole.ProjectLeader ? "Prosjektleder" : "Feltarbeider",
            e.CreatedAt, null,
            (decimal)(hoursThisMonth.FirstOrDefault(h => h.PersonId == e.PersonId)?.Hours ?? 0)
        )).ToList();

        // Get invoices
        var invoices = await db.Invoices
            .IgnoreQueryFilters()
            .Where(i => i.TenantId == id && !i.IsDeleted)
            .OrderByDescending(i => i.Year).ThenByDescending(i => i.Month)
            .Select(i => new TenantInvoiceDto(
                i.Id, i.InvoiceNumber, i.Year, i.Month, i.CustomerName,
                i.AdminCount, i.WorkerCount, i.SubcontractorCount,
                i.BasePriceKr, i.ExtraUsersKr, i.SubcontractorsKr, i.TemplatesKr,
                i.DiscountKr, i.DiscountReason,
                i.TotalExVatKr, i.VatKr, i.TotalIncVatKr,
                i.Status.ToString(), i.SentAt, i.PaidAt, i.DueDate,
                i.IsCouponApplied, i.CreatedAt))
            .ToListAsync(ct);

        // Build overview (reuse list logic for single tenant)
        var ms = employees.GroupBy(e => e.Role).ToDictionary(g => g.Key, g => g.Count());
        var admins = ms.GetValueOrDefault(TenantRole.TenantAdmin);
        var pls = ms.GetValueOrDefault(TenantRole.ProjectLeader);
        var fws = ms.GetValueOrDefault(TenantRole.FieldWorker);

        var projectCount = await db.Projects.IgnoreQueryFilters().CountAsync(p => p.TenantId == id && !p.IsDeleted, ct);
        var jobCount = await db.Jobs.IgnoreQueryFilters().CountAsync(j => j.TenantId == id && !j.IsDeleted, ct);
        var templateCount = await db.ChecklistTemplates.IgnoreQueryFilters().CountAsync(t => t.TenantId == id && !t.IsDeleted, ct);
        var instanceCount = await db.ChecklistInstances.IgnoreQueryFilters().CountAsync(i => i.TenantId == id && !i.IsDeleted, ct);
        var deviationTotal = await db.Deviations.IgnoreQueryFilters().CountAsync(d => d.TenantId == id && !d.IsDeleted, ct);
        var deviationOpen = await db.Deviations.IgnoreQueryFilters().CountAsync(d => d.TenantId == id && !d.IsDeleted && d.Status == DeviationStatus.Open, ct);
        var teCount = await db.TimeEntries.IgnoreQueryFilters().CountAsync(te => te.TenantId == id && !te.IsDeleted, ct);
        var totalHours = await db.TimeEntries.IgnoreQueryFilters().Where(te => te.TenantId == id && !te.IsDeleted).SumAsync(te => te.Hours, ct);
        var chemCount = await db.Chemicals.IgnoreQueryFilters().CountAsync(c => c.TenantId == id && !c.IsDeleted, ct);
        var eqCount = await db.Equipment.IgnoreQueryFilters().CountAsync(e => e.TenantId == id && !e.IsDeleted, ct);
        var purchaseCount = await db.MarketplacePurchases.IgnoreQueryFilters().CountAsync(p => p.TenantId == id && !p.IsDeleted, ct);

        var coupon = await db.CouponRedemptions.IgnoreQueryFilters()
            .Include(r => r.CouponCode)
            .Where(r => r.TenantId == id && !r.IsDeleted)
            .OrderByDescending(r => r.RedeemedAt)
            .FirstOrDefaultAsync(ct);

        var overview = new TenantOverviewDto(
            tenant.Id, tenant.Name, tenant.OrgNumber, tenant.BusinessAddress, tenant.AccentColor,
            tenant.IndustryType, tenant.CompanySize, tenant.SubscriptionTier,
            tenant.TrialStartedAt, tenant.TrialEndsAt, tenant.SubscriptionStartedAt,
            tenant.OnboardingCompleted, tenant.IsFrozen, tenant.CreatedAt,
            admins + pls + fws, admins, pls, fws, 0,
            projectCount, jobCount, templateCount, instanceCount,
            deviationTotal, deviationOpen, teCount, (decimal)totalHours,
            chemCount, eqCount, purchaseCount, null,
            PricingConfig.CalculateMonthly(admins, pls + fws, 0),
            coupon is not null, coupon?.CouponCode.Code, coupon?.CouponCode.TrialDays);

        return Results.Ok(new TenantDetailDto(overview, employeeDtos, invoices));
    }

    private static async Task<IResult> FreezeTenant(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return Results.NotFound();
        tenant.IsFrozen = true;
        tenant.FrozenAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> UnfreezeTenant(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();
        var tenant = await db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tenant is null) return Results.NotFound();
        tenant.IsFrozen = false;
        tenant.FrozenAt = null;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ── Coupons ─────────────────────────────────────

    private static async Task<IResult> ListCoupons(
        ClaimsPrincipal user, SolodocDbContext db, CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var coupons = await db.CouponCodes
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CouponCodeDto(
                c.Id, c.Code, c.Description, c.TrialDays,
                c.MaxRedemptions, c.TimesRedeemed, c.IsActive,
                c.ExpiresAt, c.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(coupons);
    }

    private static async Task<IResult> CreateCoupon(
        CreateCouponRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var exists = await db.CouponCodes.AnyAsync(c => c.Code == request.Code && !c.IsDeleted, ct);
        if (exists) return Results.BadRequest(new { error = "Kupongkode finnes allerede." });

        var coupon = new CouponCode
        {
            Code = request.Code.ToUpperInvariant(),
            Description = request.Description ?? "",
            TrialDays = request.TrialDays,
            MaxRedemptions = request.MaxRedemptions
        };
        db.CouponCodes.Add(coupon);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new CouponCodeDto(
            coupon.Id, coupon.Code, coupon.Description, coupon.TrialDays,
            coupon.MaxRedemptions, coupon.TimesRedeemed, coupon.IsActive,
            coupon.ExpiresAt, coupon.CreatedAt));
    }

    private static async Task<IResult> DeleteCoupon(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();
        var coupon = await db.CouponCodes.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (coupon is null) return Results.NotFound();
        coupon.IsActive = false;
        coupon.IsDeleted = true;
        coupon.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── Invoices ────────────────────────────────────

    private static async Task<IResult> ListInvoices(
        ClaimsPrincipal user, SolodocDbContext db, CancellationToken ct,
        int? year = null, string? status = null)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var query = db.Invoices.IgnoreQueryFilters().Where(i => !i.IsDeleted);
        if (year.HasValue) query = query.Where(i => i.Year == year.Value);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, true, out var s))
            query = query.Where(i => i.Status == s);

        var invoices = await query
            .OrderByDescending(i => i.Year).ThenByDescending(i => i.Month).ThenBy(i => i.CustomerName)
            .Select(i => new TenantInvoiceDto(
                i.Id, i.InvoiceNumber, i.Year, i.Month, i.CustomerName,
                i.AdminCount, i.WorkerCount, i.SubcontractorCount,
                i.BasePriceKr, i.ExtraUsersKr, i.SubcontractorsKr, i.TemplatesKr,
                i.DiscountKr, i.DiscountReason,
                i.TotalExVatKr, i.VatKr, i.TotalIncVatKr,
                i.Status.ToString(), i.SentAt, i.PaidAt, i.DueDate,
                i.IsCouponApplied, i.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(invoices);
    }

    private static async Task<IResult> GenerateMonthlyInvoices(
        GenerateInvoicesRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var tenants = await db.Tenants.IgnoreQueryFilters()
            .Where(t => !t.IsDeleted && !t.IsFrozen)
            .ToListAsync(ct);

        var generated = 0;
        var lastInvoiceNum = await db.Invoices.IgnoreQueryFilters()
            .Where(i => i.Year == request.Year)
            .OrderByDescending(i => i.InvoiceNumber)
            .Select(i => i.InvoiceNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (lastInvoiceNum is not null)
        {
            var parts = lastInvoiceNum.Split('-');
            if (parts.Length == 3 && int.TryParse(parts[2], out var lastSeq))
                seq = lastSeq + 1;
        }

        foreach (var tenant in tenants)
        {
            // Skip if already invoiced
            var alreadyInvoiced = await db.Invoices.IgnoreQueryFilters()
                .AnyAsync(i => i.TenantId == tenant.Id && i.Year == request.Year && i.Month == request.Month && !i.IsDeleted, ct);
            if (alreadyInvoiced) continue;

            // Check coupon
            var coupon = await db.CouponRedemptions.IgnoreQueryFilters()
                .Include(r => r.CouponCode)
                .Where(r => r.TenantId == tenant.Id && !r.IsDeleted)
                .OrderByDescending(r => r.RedeemedAt)
                .FirstOrDefaultAsync(ct);

            var hasCoupon = false;
            if (coupon is not null && tenant.TrialEndsAt.HasValue && tenant.TrialEndsAt > DateTimeOffset.UtcNow)
                hasCoupon = true;

            // Count users
            var ms = await db.TenantMemberships.IgnoreQueryFilters()
                .Where(m => m.TenantId == tenant.Id && m.State == TenantMembershipState.Active && !m.IsDeleted)
                .GroupBy(m => m.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var admins = ms.FirstOrDefault(m => m.Role == TenantRole.TenantAdmin)?.Count ?? 0;
            var workers = (ms.FirstOrDefault(m => m.Role == TenantRole.ProjectLeader)?.Count ?? 0)
                        + (ms.FirstOrDefault(m => m.Role == TenantRole.FieldWorker)?.Count ?? 0);
            var subs = 0;

            var totalRegular = admins + workers;
            var extraUsers = Math.Max(0, totalRegular - PricingConfig.BaseIncludedUsers);
            var basePr = PricingConfig.BasePriceMonthly;
            var extraPr = extraUsers * PricingConfig.ExtraUserPrice;
            var subPr = subs * PricingConfig.SubcontractorPrice;

            // Template purchases this month
            var templatePurchaseCount = await db.MarketplacePurchases.IgnoreQueryFilters()
                .CountAsync(p => p.TenantId == tenant.Id && !p.IsDeleted
                    && p.PurchasedAt.Year == request.Year && p.PurchasedAt.Month == request.Month, ct);
            var templatePr = templatePurchaseCount * PricingConfig.SingleTemplatePrice;

            var totalEx = basePr + extraPr + subPr + templatePr;
            var discount = hasCoupon ? totalEx : 0;
            var discountReason = hasCoupon ? $"Kupong: {coupon!.CouponCode.Code}" : null;
            totalEx -= discount;
            var vat = (int)Math.Round(totalEx * 0.25m, MidpointRounding.AwayFromZero);
            var totalInc = totalEx + vat;

            var invoice = new Invoice
            {
                TenantId = tenant.Id,
                InvoiceNumber = $"SOL-{request.Year}-{seq:D4}",
                Year = request.Year,
                Month = request.Month,
                CustomerName = tenant.Name,
                CustomerOrgNumber = tenant.OrgNumber,
                CustomerAddress = tenant.BusinessAddress,
                AdminCount = admins,
                WorkerCount = workers,
                SubcontractorCount = subs,
                TemplatePurchases = templatePurchaseCount,
                BasePriceKr = basePr,
                ExtraUsersKr = extraPr,
                SubcontractorsKr = subPr,
                TemplatesKr = templatePr,
                DiscountKr = discount,
                DiscountReason = discountReason,
                TotalExVatKr = totalEx,
                VatKr = vat,
                TotalIncVatKr = totalInc,
                Status = InvoiceStatus.Draft,
                DueDate = new DateTimeOffset(request.Year, request.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(1).AddDays(14),
                IsCouponApplied = hasCoupon,
                CouponCode = hasCoupon ? coupon!.CouponCode.Code : null
            };
            db.Invoices.Add(invoice);
            seq++;
            generated++;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { generated });
    }

    private static async Task<IResult> UpdateInvoiceStatus(
        Guid id,
        UpdateInvoiceStatusRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var invoice = await db.Invoices.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);
        if (invoice is null) return Results.NotFound();

        if (!Enum.TryParse<InvoiceStatus>(request.Status, true, out var newStatus))
            return Results.BadRequest(new { error = "Ugyldig status" });

        invoice.Status = newStatus;
        if (newStatus == InvoiceStatus.Sent) invoice.SentAt = DateTimeOffset.UtcNow;
        if (newStatus == InvoiceStatus.Paid) invoice.PaidAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DownloadEhfXml(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var invoice = await db.Invoices.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, ct);
        if (invoice is null) return Results.NotFound();

        var xml = GenerateEhfXml(invoice);
        return Results.Content(xml, "application/xml");
    }

    // ── EHF XML Generator (PEPPOL BIS 3.0) ─────────

    private static string GenerateEhfXml(Invoice inv)
    {
        var issueDate = inv.CreatedAt.ToString("yyyy-MM-dd");
        var dueDate = inv.DueDate?.ToString("yyyy-MM-dd") ?? inv.CreatedAt.AddDays(30).ToString("yyyy-MM-dd");
        var periodStart = new DateOnly(inv.Year, inv.Month, 1).ToString("yyyy-MM-dd");
        var periodEnd = new DateOnly(inv.Year, inv.Month, 1).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");

        var lines = new List<string>();
        var lineNum = 1;

        if (inv.BasePriceKr > 0)
            lines.Add(EhfLine(lineNum++, "Solodoc abonnement - grunnpakke", 1, inv.BasePriceKr));
        if (inv.ExtraUsersKr > 0)
        {
            var extraUsers = inv.ExtraUsersKr / PricingConfig.ExtraUserPrice;
            lines.Add(EhfLine(lineNum++, $"Ekstra brukere ({extraUsers} stk)", extraUsers, PricingConfig.ExtraUserPrice));
        }
        if (inv.SubcontractorsKr > 0)
        {
            var subCount = inv.SubcontractorCount;
            lines.Add(EhfLine(lineNum++, $"Underentreprenorer ({subCount} stk)", subCount, PricingConfig.SubcontractorPrice));
        }
        if (inv.TemplatesKr > 0)
            lines.Add(EhfLine(lineNum++, $"Maler fra malbutikk ({inv.TemplatePurchases} stk)", inv.TemplatePurchases, PricingConfig.SingleTemplatePrice));
        if (inv.DiscountKr > 0)
            lines.Add(EhfLine(lineNum, $"Rabatt: {inv.DiscountReason}", 1, -inv.DiscountKr));

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Invoice xmlns=""urn:oasis:names:specification:ubl:schema:xsd:Invoice-2""
         xmlns:cac=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2""
         xmlns:cbc=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"">
  <cbc:CustomizationID>urn:cen.eu:en16931:2017#compliant#urn:fdc:peppol.eu:2017:poacc:billing:3.0</cbc:CustomizationID>
  <cbc:ProfileID>urn:fdc:peppol.eu:2017:poacc:billing:01:1.0</cbc:ProfileID>
  <cbc:ID>{inv.InvoiceNumber}</cbc:ID>
  <cbc:IssueDate>{issueDate}</cbc:IssueDate>
  <cbc:DueDate>{dueDate}</cbc:DueDate>
  <cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode>
  <cbc:DocumentCurrencyCode>NOK</cbc:DocumentCurrencyCode>
  <cac:InvoicePeriod>
    <cbc:StartDate>{periodStart}</cbc:StartDate>
    <cbc:EndDate>{periodEnd}</cbc:EndDate>
  </cac:InvoicePeriod>
  <cac:AccountingSupplierParty>
    <cac:Party>
      <cbc:EndpointID schemeID=""0192"">933826638</cbc:EndpointID>
      <cac:PartyIdentification><cbc:ID>933826638</cbc:ID></cac:PartyIdentification>
      <cac:PartyName><cbc:Name>Solodoc AS</cbc:Name></cac:PartyName>
      <cac:PostalAddress>
        <cbc:StreetName>Solodoc HQ</cbc:StreetName>
        <cbc:CityName>Bergen</cbc:CityName>
        <cbc:PostalZone>5003</cbc:PostalZone>
        <cac:Country><cbc:IdentificationCode>NO</cbc:IdentificationCode></cac:Country>
      </cac:PostalAddress>
      <cac:PartyTaxScheme>
        <cbc:CompanyID>NO933826638MVA</cbc:CompanyID>
        <cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme>
      </cac:PartyTaxScheme>
      <cac:PartyLegalEntity><cbc:RegistrationName>Solodoc AS</cbc:RegistrationName><cbc:CompanyID>933826638</cbc:CompanyID></cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingSupplierParty>
  <cac:AccountingCustomerParty>
    <cac:Party>
      <cbc:EndpointID schemeID=""0192"">{EscapeXml(inv.CustomerOrgNumber)}</cbc:EndpointID>
      <cac:PartyIdentification><cbc:ID>{EscapeXml(inv.CustomerOrgNumber)}</cbc:ID></cac:PartyIdentification>
      <cac:PartyName><cbc:Name>{EscapeXml(inv.CustomerName)}</cbc:Name></cac:PartyName>
      <cac:PostalAddress>
        <cbc:StreetName>{EscapeXml(inv.CustomerAddress ?? "")}</cbc:StreetName>
        <cac:Country><cbc:IdentificationCode>NO</cbc:IdentificationCode></cac:Country>
      </cac:PostalAddress>
      <cac:PartyTaxScheme>
        <cbc:CompanyID>NO{EscapeXml(inv.CustomerOrgNumber)}MVA</cbc:CompanyID>
        <cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme>
      </cac:PartyTaxScheme>
      <cac:PartyLegalEntity><cbc:RegistrationName>{EscapeXml(inv.CustomerName)}</cbc:RegistrationName><cbc:CompanyID>{EscapeXml(inv.CustomerOrgNumber)}</cbc:CompanyID></cac:PartyLegalEntity>
    </cac:Party>
  </cac:AccountingCustomerParty>
  <cac:PaymentMeans>
    <cbc:PaymentMeansCode>30</cbc:PaymentMeansCode>
    <cbc:PaymentID>{inv.InvoiceNumber}</cbc:PaymentID>
    <cac:PayeeFinancialAccount>
      <cbc:ID>12345678901</cbc:ID> <!-- TODO: Replace with real Solodoc bank account -->
    </cac:PayeeFinancialAccount>
  </cac:PaymentMeans>
  <cac:TaxTotal>
    <cbc:TaxAmount currencyID=""NOK"">{inv.VatKr}.00</cbc:TaxAmount>
    <cac:TaxSubtotal>
      <cbc:TaxableAmount currencyID=""NOK"">{inv.TotalExVatKr}.00</cbc:TaxableAmount>
      <cbc:TaxAmount currencyID=""NOK"">{inv.VatKr}.00</cbc:TaxAmount>
      <cac:TaxCategory>
        <cbc:ID>S</cbc:ID>
        <cbc:Percent>25</cbc:Percent>
        <cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme>
      </cac:TaxCategory>
    </cac:TaxSubtotal>
  </cac:TaxTotal>
  <cac:LegalMonetaryTotal>
    <cbc:LineExtensionAmount currencyID=""NOK"">{inv.TotalExVatKr}.00</cbc:LineExtensionAmount>
    <cbc:TaxExclusiveAmount currencyID=""NOK"">{inv.TotalExVatKr}.00</cbc:TaxExclusiveAmount>
    <cbc:TaxInclusiveAmount currencyID=""NOK"">{inv.TotalIncVatKr}.00</cbc:TaxInclusiveAmount>
    <cbc:PayableAmount currencyID=""NOK"">{inv.TotalIncVatKr}.00</cbc:PayableAmount>
  </cac:LegalMonetaryTotal>
  {string.Join("\n  ", lines)}
</Invoice>";
    }

    private static string EhfLine(int num, string desc, int qty, int unitPrice)
    {
        var lineTotal = qty * unitPrice;
        var vat = (int)Math.Round(lineTotal * 0.25m, MidpointRounding.AwayFromZero);
        return $@"<cac:InvoiceLine>
    <cbc:ID>{num}</cbc:ID>
    <cbc:InvoicedQuantity unitCode=""EA"">{qty}</cbc:InvoicedQuantity>
    <cbc:LineExtensionAmount currencyID=""NOK"">{lineTotal}.00</cbc:LineExtensionAmount>
    <cac:Item>
      <cbc:Name>{EscapeXml(desc)}</cbc:Name>
      <cac:ClassifiedTaxCategory>
        <cbc:ID>S</cbc:ID>
        <cbc:Percent>25</cbc:Percent>
        <cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme>
      </cac:ClassifiedTaxCategory>
    </cac:Item>
    <cac:Price><cbc:PriceAmount currencyID=""NOK"">{unitPrice}.00</cbc:PriceAmount></cac:Price>
  </cac:InvoiceLine>";
    }

    private static string EscapeXml(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ── Client errors ───────────────────────────────

    private static async Task<IResult> ReportClientError(
        ReportErrorRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        var personId = Guid.TryParse(
            user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var pid)
            ? (Guid?)pid : null;

        db.ClientErrors.Add(new ClientError
        {
            TenantId = tp.TenantId,
            PersonId = personId,
            UserEmail = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email"),
            Message = request.Message[..Math.Min(request.Message.Length, 2000)],
            StackTrace = request.StackTrace?[..Math.Min(request.StackTrace.Length, 8000)],
            Page = request.Page,
            UserAgent = request.UserAgent,
            AdditionalInfo = request.AdditionalInfo
        });
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ListClientErrors(
        ClaimsPrincipal user, SolodocDbContext db, CancellationToken ct,
        bool includeResolved = false)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();

        var query = db.ClientErrors.IgnoreQueryFilters().Where(e => !e.IsDeleted);
        if (!includeResolved) query = query.Where(e => !e.IsResolved);

        var errors = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(200)
            .Select(e => new ClientErrorDto(
                e.Id, e.TenantId, e.UserEmail, e.Message, e.StackTrace,
                e.Page, e.UserAgent, e.IsResolved, e.CreatedAt))
            .ToListAsync(ct);

        return Results.Ok(errors);
    }

    private static async Task<IResult> ResolveClientError(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();
        var error = await db.ClientErrors.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (error is null) return Results.NotFound();
        error.IsResolved = true;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
