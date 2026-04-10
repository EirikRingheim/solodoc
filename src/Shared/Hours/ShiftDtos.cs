namespace Solodoc.Shared.Hours;

// ─── Shift Definitions ───────────────────────────────
public record ShiftDefinitionDto(
    Guid Id,
    string Name,
    string Color,
    bool IsWorkDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int BreakMinutes,
    decimal NormalHours,
    bool IsActive);

public record CreateShiftDefinitionRequest(
    string Name,
    string Color,
    bool IsWorkDay,
    string? StartTime,   // HH:mm string
    string? EndTime,
    int BreakMinutes);

// ─── Rotation Patterns ───────────────────────────────
public record RotationPatternDto(
    Guid Id,
    string Name,
    int CycleLengthDays,
    List<RotationPatternDayDto> Days);

public record RotationPatternDayDto(
    int DayInCycle,
    Guid ShiftDefinitionId,
    string ShiftName,
    string ShiftColor,
    bool IsWorkDay);

public record CreateRotationPatternRequest(
    string Name,
    int CycleLengthDays,
    List<RotationDayInput> Days);

public record RotationDayInput(
    int DayInCycle,
    Guid ShiftDefinitionId);

// ─── Employee Rotation Assignment ────────────────────
public record EmployeeRotationAssignmentDto(
    Guid Id,
    Guid PersonId,
    string EmployeeName,
    Guid RotationPatternId,
    string PatternName,
    DateOnly CycleStartDate,
    DateOnly? EffectiveTo);

public record AssignRotationRequest(
    Guid PersonId,
    Guid RotationPatternId,
    DateOnly CycleStartDate);

// ─── Today's Shift (for worker view) ────────────────
public record TodayShiftDto(
    string? ShiftName,
    string? ShiftColor,
    bool IsWorkDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    decimal NormalHours,
    int DayInCycle,
    string? RotationName);

// ─── Overtime Rules ──────────────────────────────────
public record OvertimeRuleDto(
    Guid Id,
    string Name,
    int Priority,
    decimal RatePercent,
    bool AppliesToWeekdays,
    bool AppliesToSaturday,
    bool AppliesToSunday,
    bool AppliesToRedDays,
    string? TimeRangeStart,
    string? TimeRangeEnd,
    Guid? ShiftDefinitionId,
    string? ShiftName,
    bool IsActive);

public record CreateOvertimeRuleRequest(
    string Name,
    int Priority,
    decimal RatePercent,
    bool AppliesToWeekdays,
    bool AppliesToSaturday,
    bool AppliesToSunday,
    bool AppliesToRedDays,
    string? TimeRangeStart,
    string? TimeRangeEnd,
    Guid? ShiftDefinitionId);

// ─── Planner (admin drag assignment) ─────────────────
public record PlannerRowDto(
    Guid PersonId,
    string EmployeeName,
    List<PlannerCellDto> Days);

public record PlannerCellDto(
    DateOnly Date,
    string? ShiftName,
    string? ShiftColor,
    bool IsWorkDay,
    string? AbsenceType,
    string? ProjectName,
    string? JobDescription,
    decimal Hours,
    string HoursStatus);  // "Godkjent", "Registrert", "Mangler", ""

public record PlannerAssignRequest(
    Guid PersonId,
    DateOnly FromDate,
    DateOnly ToDate,
    string AssignType,        // "shift", "project", "absence"
    Guid? ShiftDefinitionId,  // for shift assignment
    Guid? ProjectId,          // optional project
    Guid? JobId,              // optional job
    string? AbsenceType);     // for absence assignment

// ─── Employee Calendar ───────────────────────────────
public record EmployeeCalendarDto(
    string EmployeeName,
    List<EmployeeCalendarDayDto> Days);

public record EmployeeCalendarDayDto(
    DateOnly Date,
    string? ShiftName,
    string? ShiftColor,
    string? ProjectName,
    string? AbsenceType,
    decimal Hours,
    string Status);

// ─── Tenant Hours Settings ──────────────────────────
public record HoursSettingsDto(
    bool TimebankEnabled,
    bool OvertimeStackingMode);

public record UpdateHoursSettingsRequest(
    bool TimebankEnabled,
    bool OvertimeStackingMode);
