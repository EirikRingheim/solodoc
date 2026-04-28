namespace Solodoc.Shared.Hours;

public record TimeEntryListItemDto(
    Guid Id,
    DateOnly Date,
    decimal Hours,
    decimal OvertimeHours,
    int BreakMinutes,
    string Category,
    string Status,
    string? EmployeeName,
    Guid? ProjectId,
    string? ProjectName,
    Guid? JobId,
    string? JobDescription,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    string? Notes,
    bool IsManual,
    List<TimeEntryAllowanceTagDto> Allowances);

public record TimeEntryAllowanceTagDto(
    string RuleName,
    string Type,
    decimal Hours,
    decimal Amount);

public record TimeEntryDetailDto(
    Guid Id,
    DateOnly Date,
    decimal Hours,
    decimal OvertimeHours,
    int BreakMinutes,
    string Category,
    string Status,
    string? ProjectName,
    Guid? ProjectId,
    string? JobDescription,
    Guid? JobId,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime,
    string? Notes,
    bool IsManual,
    List<TimeEntryAllowanceDto> Allowances);

public record TimeEntryAllowanceDto(
    Guid Id,
    string RuleName,
    decimal Hours,
    decimal Amount);

public record ClockInRequest(
    Guid? ProjectId,
    Guid? JobId,
    string? Category,
    double? Latitude,
    double? Longitude,
    DateTimeOffset? StartTime = null);

public record ClockOutRequest(
    int? BreakMinutes,
    double? Latitude,
    double? Longitude = null);

public record ManualTimeEntryRequest(
    DateOnly Date,
    decimal? Hours,
    Guid? ProjectId,
    Guid? JobId,
    string? Category,
    string? Notes,
    DateTimeOffset? StartTime = null,
    DateTimeOffset? EndTime = null,
    int? BreakMinutes = null);

public record SubmitHoursRequest(
    List<Guid> TimeEntryIds);

public record ApproveRejectRequest(
    string? Reason);

public record ActiveClockDto(
    Guid TimeEntryId,
    DateTimeOffset StartTime,
    string? ProjectName,
    Guid? ProjectId);

public record WorkScheduleDto(
    Guid Id,
    string Name,
    decimal WeeklyHours,
    int DefaultBreakMinutes,
    bool IsActive);

public record CreateScheduleRequest(
    string Name,
    decimal WeeklyHours,
    int DefaultBreakMinutes);

public record AllowanceRuleDto(
    Guid Id,
    string Name,
    string Type,
    string AmountType,
    decimal Amount,
    TimeOnly? TimeRangeStart,
    TimeOnly? TimeRangeEnd,
    bool IsActive);

// ─── My Schedule ─────────────────────────────────────────
public record MyScheduleDto(
    Guid? ScheduleId,
    string? ScheduleName,
    decimal WeeklyHours,
    int DefaultBreakMinutes);

// ─── Add Allowance to Time Entry ─────────────────────────
public record AddTimeEntryAllowanceRequest(
    Guid AllowanceRuleId,
    decimal? Hours,
    string? Notes);

// ─── Overtime Bank Credit ─────────────────────────────
public record CreditOvertimeBankRequest(
    Guid TimeEntryId,
    decimal Hours);

// ─── Heatmap Summary ─────────────────────────────────────
public record HoursHeatmapRequest(
    DateOnly From,
    DateOnly To);

public record HoursHeatmapDto(
    List<HeatmapEmployeeRow> Rows,
    List<DateOnly> Dates);

public record HeatmapEmployeeRow(
    Guid PersonId,
    string EmployeeName,
    List<HeatmapCell> Cells);

public record HeatmapCell(
    DateOnly Date,
    decimal Hours,
    decimal OvertimeHours,
    string Status, // "Godkjent", "Registrert", "Mangler", "Fravaer"
    string? AbsenceType); // null if no absence

// ─── My Heatmap (Employee View) ──────────────────────────
public record MyHeatmapDto(
    List<DateOnly> Dates,
    List<MyHeatmapCell> Cells);

public record MyHeatmapCell(
    DateOnly Date,
    decimal Hours,
    string Status, // "Godkjent", "Registrert", "Mangler", "Fravaer"
    string? AbsenceType); // "Ferie", "Syk", etc. — null if no absence

// ─── Admin Day Detail ────────────────────────────────────
public record DayDetailDto(
    string EmployeeName,
    DateOnly Date,
    decimal TotalHours,
    decimal TotalOvertime,
    string DayStatus,
    List<TimeEntryListItemDto> Entries);

// ─── Admin Batch Approve ─────────────────────────────────
public record ApproveDayRequest(
    Guid PersonId,
    DateOnly Date);
