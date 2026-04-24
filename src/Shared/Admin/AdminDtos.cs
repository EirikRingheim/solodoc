namespace Solodoc.Shared.Admin;

// ── Tenant overview for super admin ──────────────────

public record TenantOverviewDto(
    Guid Id,
    string Name,
    string OrgNumber,
    string? BusinessAddress,
    string? AccentColor,
    string? IndustryType,
    string? CompanySize,
    string SubscriptionTier,
    DateTimeOffset? TrialStartedAt,
    DateTimeOffset? TrialEndsAt,
    DateTimeOffset? SubscriptionStartedAt,
    bool OnboardingCompleted,
    bool IsFrozen,
    DateTimeOffset CreatedAt,
    // Usage stats
    int EmployeeCount,
    int AdminCount,
    int ProjectLeaderCount,
    int FieldWorkerCount,
    int SubcontractorCount,
    int ProjectCount,
    int JobCount,
    int ChecklistTemplateCount,
    int ChecklistInstanceCount,
    int DeviationCount,
    int OpenDeviationCount,
    int TimeEntryCount,
    decimal TotalHoursLogged,
    int ChemicalCount,
    int EquipmentCount,
    int MarketplacePurchaseCount,
    // Activity
    DateTimeOffset? LastActivityAt,
    // Billing
    int MonthlyEstimateKr,
    bool HasCoupon,
    string? CouponCode,
    int? CouponTrialDays);

public record TenantDetailDto(
    TenantOverviewDto Overview,
    List<TenantEmployeeDto> Employees,
    List<TenantInvoiceDto> Invoices);

public record TenantEmployeeDto(
    Guid PersonId,
    string FullName,
    string Email,
    string Role,
    DateTimeOffset JoinedAt,
    DateTimeOffset? LastLoginAt,
    decimal HoursThisMonth);

// ── Coupon codes ─────────────────────────────────────

public record CouponCodeDto(
    Guid Id,
    string Code,
    string Description,
    int TrialDays,
    int MaxRedemptions,
    int TimesRedeemed,
    bool IsActive,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);

public record CreateCouponRequest(
    string Code,
    string? Description,
    int TrialDays = 365,
    int MaxRedemptions = 0);

public record RedeemCouponRequest(string Code);

public record RedeemCouponResult(bool Success, string? Error, int? TrialDays);

// ── Invoices ─────────────────────────────────────────

public record TenantInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    int Year,
    int Month,
    string CustomerName,
    int AdminCount,
    int WorkerCount,
    int SubcontractorCount,
    int BasePriceKr,
    int ExtraUsersKr,
    int SubcontractorsKr,
    int TemplatesKr,
    int DiscountKr,
    string? DiscountReason,
    int TotalExVatKr,
    int VatKr,
    int TotalIncVatKr,
    string Status,
    DateTimeOffset? SentAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? DueDate,
    bool IsCouponApplied,
    DateTimeOffset CreatedAt);

public record GenerateInvoicesRequest(int Year, int Month);

// ── Client errors ────────────────────────────────────

public record ClientErrorDto(
    Guid Id,
    Guid? TenantId,
    string? UserEmail,
    string Message,
    string? StackTrace,
    string? Page,
    string? UserAgent,
    bool IsResolved,
    DateTimeOffset CreatedAt);

public record ReportErrorRequest(
    string Message,
    string? StackTrace,
    string? Page,
    string? UserAgent,
    string? AdditionalInfo);

public record UpdateInvoiceStatusRequest(string Status);

// ── Pay period ────────────────────────────────────────

public record PayPeriodUpdateRequest(int StartDay);

public record PayPeriodCurrentDto(
    int StartDay,
    string PeriodStart,
    string PeriodEnd);
