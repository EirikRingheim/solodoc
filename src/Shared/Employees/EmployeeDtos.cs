namespace Solodoc.Shared.Employees;

// ─── Employee List & Detail ────────────────────────────────────────

public record EmployeeListItemDto(
    Guid PersonId,
    string FullName,
    string Email,
    string Role,
    int TotalCerts,
    int ExpiringCerts,
    int ExpiredCerts);

public record EmployeeDetailDto(
    Guid PersonId,
    string FullName,
    string Email,
    string? Phone,
    string Role,
    string? TimeZoneId,
    List<CertificationDto> Certifications,
    List<TrainingDto> Trainings);

// ─── Certifications ────────────────────────────────────────────────

public record CertificationDto(
    Guid Id,
    string Name,
    string? Type,
    string? IssuedBy,
    DateOnly? ExpiryDate,
    bool IsExpired,
    bool IsExpiringSoon,
    string? FileKey);

public record CreateCertificationRequest(
    string Name,
    string? Type,
    string? IssuedBy,
    DateOnly? ExpiryDate,
    string? FileKey = null);

// ─── Training ──────────────────────────────────────────────────────

public record TrainingDto(
    Guid Id,
    string Topic,
    string TrainerName,
    DateOnly Date,
    decimal? DurationHours);

public record CreateTrainingRequest(
    string Topic,
    Guid TrainerId,
    DateOnly Date,
    decimal? DurationHours,
    string? Notes);

// ─── Profile (Self-Service) ────────────────────────────────────────

public record ProfileDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    string? TimeZoneId);

public record UpdateProfileRequest(
    string FullName,
    string? Phone,
    string? TimeZoneId);

// ─── Vacation ──────────────────────────────────────────────────────

public record VacationBalanceDto(
    int Year,
    decimal AnnualAllowanceDays,
    decimal CarriedOverDays,
    decimal UsedDays,
    decimal RemainingDays);

public record CreateVacationEntryRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Days);

public record VacationEntryDto(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Days,
    string Status,
    string? ApprovedBy,
    DateTimeOffset? ApprovedAt);

// ─── Vacation Response ─────────────────────────────────────────────

public record VacationOverviewDto(
    VacationBalanceDto? Balance,
    List<VacationEntryDto> Entries);

// ─── Invite ────────────────────────────────────────────────────────

public record InviteEmployeeRequest(
    string Email,
    string Role);

public record ChangeRoleRequest(string Role);
