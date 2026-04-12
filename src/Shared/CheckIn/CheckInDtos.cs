namespace Solodoc.Shared.CheckIn;

// ─── Check-in/out requests ───────────────────────────
public record CheckInRequest(
    Guid? ProjectId,
    Guid? JobId,
    Guid? LocationId,
    string Source,  // "Manual", "QrCode", "TimeClock"
    double? Latitude,
    double? Longitude);

public record CheckOutRequest(
    double? Latitude,
    double? Longitude);

// ─── Current status ──────────────────────────────────
public record MyCheckInStatusDto(
    Guid? CheckInId,
    bool IsCheckedIn,
    string? SiteName,
    string? SiteType,  // "Prosjekt", "Oppdrag", "Lokasjon"
    DateTimeOffset? CheckInAt);

// ─── Who's on site ───────────────────────────────────
public record OnSitePersonDto(
    Guid PersonId,
    string FullName,
    string Role,
    string? Company,  // For subcontractors
    DateTimeOffset CheckInAt,
    string Source);

// ─── All sites overview (main dashboard) ─────────────
public record SiteOverviewDto(
    Guid SiteId,
    string SiteName,
    string SiteType,  // "Prosjekt", "Oppdrag", "Lokasjon"
    int PersonCount,
    List<OnSitePersonDto> Persons);

// ─── Check-in history ────────────────────────────────
public record CheckInHistoryDto(
    Guid Id,
    string PersonName,
    string? Role,
    DateTimeOffset CheckInAt,
    DateTimeOffset? CheckOutAt,
    string Source,
    bool IsAutoCheckout,
    double? GpsLatIn,
    double? GpsLongIn);

// ─── My check-in log ────────────────────────────────
public record CheckInLogEntryDto(
    Guid Id,
    string SiteName,
    string SiteType,
    DateTimeOffset CheckInAt,
    DateTimeOffset? CheckOutAt,
    string Source,
    int DurationMinutes);

// ─── Guest check-in (no account needed) ─────────────
public record GuestCheckInRequest(
    string Name,
    string? Company,
    string? Phone,
    string Slug);

// ─── QR Code ─────────────────────────────────────────
public record QrCodeInfoDto(
    string Slug,
    string Url,
    string SiteName,
    string SiteType,
    Guid? SiteId = null,
    string? TenantName = null,
    string? AccentColor = null,
    string? LogoUrl = null);
