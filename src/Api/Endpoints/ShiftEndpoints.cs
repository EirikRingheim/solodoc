using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Hours;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Hours;

namespace Solodoc.Api.Endpoints;

public static class ShiftEndpoints
{
    public static WebApplication MapShiftEndpoints(this WebApplication app)
    {
        // Shift definitions
        app.MapGet("/api/shifts", ListShiftDefinitions).RequireAuthorization();
        app.MapPost("/api/shifts", CreateShiftDefinition).RequireAuthorization();
        app.MapPut("/api/shifts/{id:guid}", UpdateShiftDefinition).RequireAuthorization();
        app.MapDelete("/api/shifts/{id:guid}", DeleteShiftDefinition).RequireAuthorization();

        // Rotation patterns
        app.MapGet("/api/rotations", ListRotationPatterns).RequireAuthorization();
        app.MapPost("/api/rotations", CreateRotationPattern).RequireAuthorization();
        app.MapDelete("/api/rotations/{id:guid}", DeleteRotationPattern).RequireAuthorization();

        // Assignments
        app.MapGet("/api/rotations/assignments", ListRotationAssignments).RequireAuthorization();
        app.MapPost("/api/rotations/assignments", AssignRotation).RequireAuthorization();
        app.MapDelete("/api/rotations/assignments/{id:guid}", RemoveAssignment).RequireAuthorization();

        // Today's shift for current user
        app.MapGet("/api/shifts/today", GetTodayShift).RequireAuthorization();
        app.MapGet("/api/shifts/my-plan", GetMyPlan).RequireAuthorization();

        // Overtime rules
        app.MapGet("/api/overtime-rules", ListOvertimeRules).RequireAuthorization();
        app.MapPost("/api/overtime-rules", CreateOvertimeRule).RequireAuthorization();
        app.MapPut("/api/overtime-rules/{id:guid}", UpdateOvertimeRule).RequireAuthorization();
        app.MapDelete("/api/overtime-rules/{id:guid}", DeleteOvertimeRule).RequireAuthorization();

        // Planner
        app.MapGet("/api/planner", GetPlannerData).RequireAuthorization();
        app.MapPost("/api/planner/assign", PlannerAssign).RequireAuthorization();
        app.MapPost("/api/planner/clear", PlannerClear).RequireAuthorization();

        // Employee calendar
        app.MapGet("/api/planner/calendar/{personId:guid}", GetEmployeeCalendar).RequireAuthorization();

        // Hours settings
        app.MapGet("/api/hours-settings", GetHoursSettings).RequireAuthorization();
        app.MapPut("/api/hours-settings", UpdateHoursSettings).RequireAuthorization();

        return app;
    }

    private static (Guid? personId, bool valid) GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var pid) ? (pid, true) : (null, false);
    }

    // ─── Shift Definitions ──────────────────────────────────────────

    private static async Task<IResult> ListShiftDefinitions(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var items = await db.ShiftDefinitions
            .Where(s => s.TenantId == tp.TenantId.Value && s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new ShiftDefinitionDto(s.Id, s.Name, s.Color, s.IsWorkDay,
                s.StartTime, s.EndTime, s.BreakMinutes, s.NormalHours, s.IsActive))
            .ToListAsync(ct);
        return Results.Ok(items);
    }

    private static async Task<IResult> CreateShiftDefinition(
        CreateShiftDefinitionRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { error = "Navn er påkrevd." });

        TimeOnly? start = null, end = null;
        if (req.IsWorkDay)
        {
            if (!TimeOnly.TryParse(req.StartTime, out var s) || !TimeOnly.TryParse(req.EndTime, out var e))
                return Results.BadRequest(new { error = "Ugyldig start/slutttid." });
            start = s; end = e;
        }

        var normalHours = 0m;
        if (start.HasValue && end.HasValue)
        {
            var mins = (end.Value.ToTimeSpan() - start.Value.ToTimeSpan()).TotalMinutes;
            if (mins < 0) mins += 24 * 60;
            normalHours = Math.Max(0, (decimal)(mins - req.BreakMinutes) / 60m);
        }

        var shift = new ShiftDefinition
        {
            TenantId = tp.TenantId.Value,
            Name = req.Name,
            Color = req.Color,
            IsWorkDay = req.IsWorkDay,
            StartTime = start,
            EndTime = end,
            BreakMinutes = req.BreakMinutes,
            NormalHours = normalHours,
            IsActive = true
        };

        db.ShiftDefinitions.Add(shift);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/shifts/{shift.Id}", new { id = shift.Id });
    }

    private static async Task<IResult> UpdateShiftDefinition(
        Guid id, CreateShiftDefinitionRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var shift = await db.ShiftDefinitions.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tp.TenantId.Value, ct);
        if (shift is null) return Results.NotFound();

        shift.Name = req.Name;
        shift.Color = req.Color;
        shift.IsWorkDay = req.IsWorkDay;
        if (req.IsWorkDay && TimeOnly.TryParse(req.StartTime, out var s) && TimeOnly.TryParse(req.EndTime, out var e))
        {
            shift.StartTime = s; shift.EndTime = e;
            var mins = (e.ToTimeSpan() - s.ToTimeSpan()).TotalMinutes;
            if (mins < 0) mins += 24 * 60;
            shift.NormalHours = Math.Max(0, (decimal)(mins - req.BreakMinutes) / 60m);
        }
        shift.BreakMinutes = req.BreakMinutes;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteShiftDefinition(Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var shift = await db.ShiftDefinitions.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tp.TenantId.Value, ct);
        if (shift is null) return Results.NotFound();
        shift.IsActive = false;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ─── Rotation Patterns ──────────────────────────────────────────

    private static async Task<IResult> ListRotationPatterns(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var patterns = await db.RotationPatterns
            .Where(r => r.TenantId == tp.TenantId.Value)
            .Include(r => r.Days)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        var shiftIds = patterns.SelectMany(p => p.Days).Select(d => d.ShiftDefinitionId).Distinct().ToList();
        var shifts = await db.ShiftDefinitions
            .Where(s => shiftIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s, ct);

        var result = patterns.Select(p => new RotationPatternDto(
            p.Id, p.Name, p.CycleLengthDays,
            p.Days.OrderBy(d => d.DayInCycle).Select(d =>
            {
                shifts.TryGetValue(d.ShiftDefinitionId, out var shift);
                return new RotationPatternDayDto(d.DayInCycle, d.ShiftDefinitionId,
                    shift?.Name ?? "?", shift?.Color ?? "#CCC", shift?.IsWorkDay ?? false);
            }).ToList()
        )).ToList();

        return Results.Ok(result);
    }

    private static async Task<IResult> CreateRotationPattern(
        CreateRotationPatternRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { error = "Navn er påkrevd." });
        if (req.CycleLengthDays <= 0) return Results.BadRequest(new { error = "Sykluslengde ma vaere storre enn 0." });

        var pattern = new RotationPattern
        {
            TenantId = tp.TenantId.Value,
            Name = req.Name,
            CycleLengthDays = req.CycleLengthDays
        };
        db.RotationPatterns.Add(pattern);

        foreach (var day in req.Days)
        {
            db.RotationPatternDays.Add(new RotationPatternDay
            {
                RotationPatternId = pattern.Id,
                DayInCycle = day.DayInCycle,
                ShiftDefinitionId = day.ShiftDefinitionId
            });
        }

        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/rotations/{pattern.Id}", new { id = pattern.Id });
    }

    private static async Task<IResult> DeleteRotationPattern(Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var pattern = await db.RotationPatterns.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tp.TenantId.Value, ct);
        if (pattern is null) return Results.NotFound();
        var days = await db.RotationPatternDays.Where(d => d.RotationPatternId == id).ToListAsync(ct);
        db.RotationPatternDays.RemoveRange(days);
        db.RotationPatterns.Remove(pattern);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ─── Assignments ────────────────────────────────────────────────

    private static async Task<IResult> ListRotationAssignments(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var assignments = await db.EmployeeRotationAssignments
            .Join(db.RotationPatterns, a => a.RotationPatternId, p => p.Id, (a, p) => new { a, p })
            .Where(x => x.p.TenantId == tp.TenantId.Value && x.a.EffectiveTo == null)
            .Join(db.Persons, x => x.a.PersonId, p => p.Id, (x, person) => new EmployeeRotationAssignmentDto(
                x.a.Id, x.a.PersonId, person.FullName, x.a.RotationPatternId, x.p.Name,
                x.a.CycleStartDate, x.a.EffectiveTo))
            .ToListAsync(ct);

        return Results.Ok(assignments);
    }

    private static async Task<IResult> AssignRotation(
        AssignRotationRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;

        // Validate that the rotation pattern belongs to the current tenant
        var patternExists = await db.RotationPatterns
            .AnyAsync(r => r.Id == req.RotationPatternId && r.TenantId == tenantId, ct);
        if (!patternExists) return Results.NotFound();

        var assignment = new Domain.Entities.Hours.EmployeeRotationAssignment
        {
            PersonId = req.PersonId,
            RotationPatternId = req.RotationPatternId,
            CycleStartDate = req.CycleStartDate
        };
        db.EmployeeRotationAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/rotations/assignments/{assignment.Id}", new { id = assignment.Id });
    }

    private static async Task<IResult> RemoveAssignment(Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var a = await db.EmployeeRotationAssignments
            .Include(x => x.RotationPattern)
            .FirstOrDefaultAsync(x => x.Id == id && x.RotationPattern.TenantId == tp.TenantId.Value, ct);
        if (a is null) return Results.NotFound();
        a.EffectiveTo = DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ─── Today's Shift ──────────────────────────────────────────────

    private static async Task<IResult> GetTodayShift(
        ClaimsPrincipal user, SolodocDbContext db, CancellationToken ct)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid) return Results.Unauthorized();

        try {

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var assignment = await db.EmployeeRotationAssignments
            .Where(a => a.PersonId == personId!.Value && a.EffectiveTo == null)
            .Include(a => a.RotationPattern).ThenInclude(p => p.Days)
            .FirstOrDefaultAsync(ct);

        if (assignment is null)
            return Results.Ok(new TodayShiftDto(null, null, true, null, null, 7.5m, 0, null));

        var daysSinceCycleStart = (today.ToDateTime(TimeOnly.MinValue) - assignment.CycleStartDate.ToDateTime(TimeOnly.MinValue)).Days;
        var cycleDays = assignment.RotationPattern.CycleLengthDays;
        var dayInCycle = cycleDays > 0 ? (daysSinceCycleStart % cycleDays) + 1 : 1;
        if (dayInCycle < 1) dayInCycle += cycleDays;

        var patternDay = assignment.RotationPattern.Days.FirstOrDefault(d => d.DayInCycle == dayInCycle);
        if (patternDay is null)
            return Results.Ok(new TodayShiftDto(null, null, false, null, null, 0, dayInCycle, assignment.RotationPattern.Name));

        var shift = await db.ShiftDefinitions.FirstOrDefaultAsync(s => s.Id == patternDay.ShiftDefinitionId, ct);
        if (shift is null)
            return Results.Ok(new TodayShiftDto(null, null, false, null, null, 0, dayInCycle, assignment.RotationPattern.Name));

        return Results.Ok(new TodayShiftDto(
            shift.Name, shift.Color, shift.IsWorkDay,
            shift.StartTime, shift.EndTime, shift.NormalHours,
            dayInCycle, assignment.RotationPattern.Name));
        }
        catch
        {
            return Results.Ok(new TodayShiftDto(null, null, true, null, null, 7.5m, 0, null));
        }
    }

    // ─── My Plan (employee view) ───────────────────────────────────

    private static async Task<IResult> GetMyPlan(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct,
        string? from = null,
        string? to = null)
    {
        var (personId, valid) = GetPersonId(user);
        if (!valid || tp.TenantId is null) return Results.Unauthorized();

        var fromDate = DateOnly.TryParse(from, out var f) ? f : DateOnly.FromDateTime(DateTime.Today);
        var toDate = DateOnly.TryParse(to, out var t2) ? t2 : fromDate.AddDays(6);

        var entries = await db.PlannerEntries
            .Where(p => p.TenantId == tp.TenantId.Value && p.PersonId == personId!.Value
                && p.Date >= fromDate && p.Date <= toDate)
            .Select(p => new { p.Date, p.ShiftDefinitionId, p.ProjectId, p.JobId })
            .ToListAsync(ct);

        var shiftIds = entries.Select(e => e.ShiftDefinitionId).Distinct().ToList();
        var shifts = shiftIds.Count > 0
            ? await db.ShiftDefinitions.Where(s => shiftIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s, ct)
            : new Dictionary<Guid, ShiftDefinition>();

        var projIds = entries.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).Distinct().ToList();
        var projNames = projIds.Count > 0
            ? await db.Projects.Where(p => projIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();

        var jobIds = entries.Where(e => e.JobId.HasValue).Select(e => e.JobId!.Value).Distinct().ToList();
        var jobDescs = jobIds.Count > 0
            ? await db.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.Description ?? "", ct)
            : new Dictionary<Guid, string>();

        var result = entries.Select(e =>
        {
            shifts.TryGetValue(e.ShiftDefinitionId, out var shift);
            string? projName = null;
            if (e.ProjectId.HasValue && projNames.TryGetValue(e.ProjectId.Value, out var pn)) projName = pn;
            else if (e.JobId.HasValue && jobDescs.TryGetValue(e.JobId.Value, out var jd)) projName = jd;

            return new EmployeeCalendarDayDto(e.Date, shift?.Name, shift?.Color, projName, null,
                shift?.NormalHours ?? 0, shift?.IsWorkDay == true ? "Planlagt" : "Fri");
        }).ToList();

        return Results.Ok(result);
    }

    // ─── Overtime Rules ─────────────────────────────────────────────

    private static async Task<IResult> ListOvertimeRules(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var rules = await db.OvertimeRules
            .Where(r => r.TenantId == tp.TenantId.Value && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync(ct);

        var shiftIds = rules.Where(r => r.ShiftDefinitionId.HasValue).Select(r => r.ShiftDefinitionId!.Value).Distinct().ToList();
        var shiftNames = shiftIds.Count > 0
            ? await db.ShiftDefinitions.Where(s => shiftIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name, ct)
            : new Dictionary<Guid, string>();

        var dtos = rules.Select(r => new OvertimeRuleDto(
            r.Id, r.Name, r.Priority, r.RatePercent,
            r.AppliesToWeekdays, r.AppliesToSaturday, r.AppliesToSunday, r.AppliesToRedDays,
            r.TimeRangeStart?.ToString("HH:mm"), r.TimeRangeEnd?.ToString("HH:mm"),
            r.ShiftDefinitionId,
            r.ShiftDefinitionId.HasValue && shiftNames.TryGetValue(r.ShiftDefinitionId.Value, out var sn) ? sn : null,
            r.IsActive)).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> CreateOvertimeRule(
        CreateOvertimeRuleRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var rule = new OvertimeRule
        {
            TenantId = tp.TenantId.Value,
            Name = req.Name,
            Priority = req.Priority,
            RatePercent = req.RatePercent,
            AppliesToWeekdays = req.AppliesToWeekdays,
            AppliesToSaturday = req.AppliesToSaturday,
            AppliesToSunday = req.AppliesToSunday,
            AppliesToRedDays = req.AppliesToRedDays,
            TimeRangeStart = TimeOnly.TryParse(req.TimeRangeStart, out var s) ? s : null,
            TimeRangeEnd = TimeOnly.TryParse(req.TimeRangeEnd, out var e) ? e : null,
            ShiftDefinitionId = req.ShiftDefinitionId,
            IsActive = true
        };

        db.OvertimeRules.Add(rule);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/overtime-rules/{rule.Id}", new { id = rule.Id });
    }

    private static async Task<IResult> UpdateOvertimeRule(
        Guid id, CreateOvertimeRuleRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var rule = await db.OvertimeRules.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tp.TenantId.Value, ct);
        if (rule is null) return Results.NotFound();

        rule.Name = req.Name;
        rule.Priority = req.Priority;
        rule.RatePercent = req.RatePercent;
        rule.AppliesToWeekdays = req.AppliesToWeekdays;
        rule.AppliesToSaturday = req.AppliesToSaturday;
        rule.AppliesToSunday = req.AppliesToSunday;
        rule.AppliesToRedDays = req.AppliesToRedDays;
        rule.TimeRangeStart = TimeOnly.TryParse(req.TimeRangeStart, out var s) ? s : null;
        rule.TimeRangeEnd = TimeOnly.TryParse(req.TimeRangeEnd, out var e) ? e : null;
        rule.ShiftDefinitionId = req.ShiftDefinitionId;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteOvertimeRule(Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var rule = await db.OvertimeRules.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tp.TenantId.Value, ct);
        if (rule is null) return Results.NotFound();
        rule.IsActive = false;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ─── Planner ────────────────────────────────────────────────────

    private static async Task<IResult> GetPlannerData(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        string? from = null, string? to = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;

        var fromDate = DateOnly.TryParse(from, out var f) ? f : new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var toDate = DateOnly.TryParse(to, out var t) ? t : fromDate.AddMonths(1).AddDays(-1);

        // Get all active employees
        var members = await db.TenantMemberships
            .Where(m => m.TenantId == tenantId && m.State == TenantMembershipState.Active)
            .Select(m => m.PersonId).ToListAsync(ct);

        var personNames = await db.Persons
            .Where(p => members.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        // Get rotation assignments
        var assignments = await db.EmployeeRotationAssignments
            .Where(a => members.Contains(a.PersonId) && a.EffectiveTo == null)
            .Include(a => a.RotationPattern).ThenInclude(p => p.Days)
            .ToListAsync(ct);

        // Get planner entries (direct day overrides — these take priority over rotations)
        var plannerEntries = await db.PlannerEntries
            .Where(p => p.TenantId == tenantId && p.Date >= fromDate && p.Date <= toDate)
            .Select(p => new { p.PersonId, p.Date, p.ShiftDefinitionId, p.ProjectId, p.JobId })
            .ToListAsync(ct);

        // Load project and job names
        var plannerProjectIds = plannerEntries.Where(p => p.ProjectId.HasValue).Select(p => p.ProjectId!.Value).Distinct().ToList();
        var projectNames = plannerProjectIds.Count > 0
            ? await db.Projects.Where(p => plannerProjectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();

        var plannerJobIds = plannerEntries.Where(p => p.JobId.HasValue).Select(p => p.JobId!.Value).Distinct().ToList();
        var jobDescs = plannerJobIds.Count > 0
            ? await db.Jobs.Where(j => plannerJobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.Description ?? "", ct)
            : new Dictionary<Guid, string>();

        var allShiftIds = assignments.SelectMany(a => a.RotationPattern.Days).Select(d => d.ShiftDefinitionId)
            .Concat(plannerEntries.Select(p => p.ShiftDefinitionId))
            .Distinct().ToList();
        var shifts = allShiftIds.Count > 0
            ? await db.ShiftDefinitions.Where(s => allShiftIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s, ct)
            : new Dictionary<Guid, ShiftDefinition>();

        // Get time entries and absences for the period
        var entries = await db.TimeEntries
            .Where(e => e.TenantId == tenantId && e.Date >= fromDate && e.Date <= toDate)
            .Select(e => new { e.PersonId, e.Date, e.Hours, e.Status })
            .ToListAsync(ct);

        var absences = await db.Absences
            .Where(a => a.TenantId == tenantId && a.StartDate <= toDate && a.EndDate >= fromDate
                && a.Status != AbsenceStatus.Rejected)
            .Select(a => new { a.PersonId, a.Type, a.StartDate, a.EndDate })
            .ToListAsync(ct);

        var dates = new List<DateOnly>();
        for (var d = fromDate; d <= toDate; d = d.AddDays(1)) dates.Add(d);

        var rows = members.Where(pid => personNames.ContainsKey(pid)).Select(pid =>
        {
            var assignment = assignments.FirstOrDefault(a => a.PersonId == pid);

            var cells = dates.Select(date =>
            {
                var dayAbsence = absences.FirstOrDefault(a => a.PersonId == pid && a.StartDate <= date && a.EndDate >= date);
                var dayEntries = entries.Where(e => e.PersonId == pid && e.Date == date).ToList();

                string? shiftName = null, shiftColor = null, cellProjectName = null;
                var isWorkDay = date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

                // Check planner entry first (direct override)
                var plannerEntry = plannerEntries.FirstOrDefault(p => p.PersonId == pid && p.Date == date);
                if (plannerEntry is not null && shifts.TryGetValue(plannerEntry.ShiftDefinitionId, out var plannerShift))
                {
                    shiftName = plannerShift.Name;
                    shiftColor = plannerShift.Color;
                    isWorkDay = plannerShift.IsWorkDay;
                    if (plannerEntry.ProjectId.HasValue && projectNames.TryGetValue(plannerEntry.ProjectId.Value, out var pn))
                        cellProjectName = pn;
                    else if (plannerEntry.JobId.HasValue && jobDescs.TryGetValue(plannerEntry.JobId.Value, out var jd))
                        cellProjectName = jd;
                }
                // Fall back to rotation pattern
                else if (assignment is not null)
                {
                    var daysSince = (date.ToDateTime(TimeOnly.MinValue) - assignment.CycleStartDate.ToDateTime(TimeOnly.MinValue)).Days;
                    var cycle = assignment.RotationPattern.CycleLengthDays;
                    var dayInCycle = cycle > 0 ? (daysSince % cycle) + 1 : 1;
                    if (dayInCycle < 1) dayInCycle += cycle;

                    var patternDay = assignment.RotationPattern.Days.FirstOrDefault(d => d.DayInCycle == dayInCycle);
                    if (patternDay is not null && shifts.TryGetValue(patternDay.ShiftDefinitionId, out var shift))
                    {
                        shiftName = shift.Name;
                        shiftColor = shift.Color;
                        isWorkDay = shift.IsWorkDay;
                    }
                }

                string? absenceType = null;
                if (dayAbsence is not null)
                {
                    absenceType = dayAbsence.Type switch
                    {
                        AbsenceType.Ferie => "Ferie",
                        AbsenceType.Sykmelding or AbsenceType.Egenmelding => "Syk",
                        AbsenceType.Avspasering => "Avspass.",
                        _ => "Fravær"
                    };
                    // Absence takes priority — hide shift for this day
                    shiftName = null;
                    shiftColor = null;
                    cellProjectName = null;
                }

                var hours = dayEntries.Sum(e => e.Hours);
                var hoursStatus = dayAbsence is not null ? "Fravær"
                    : dayEntries.Count == 0 ? (isWorkDay ? "Mangler" : "")
                    : dayEntries.All(e => e.Status == TimeEntryStatus.Approved) ? "Godkjent"
                    : "Registrert";

                string? cellJobDesc = null;
                if (plannerEntry is not null && plannerEntry.JobId.HasValue && jobDescs.TryGetValue(plannerEntry.JobId.Value, out var jDesc))
                    cellJobDesc = jDesc;

                return new PlannerCellDto(date, shiftName, shiftColor, isWorkDay, absenceType,
                    cellProjectName, cellJobDesc, hours, hoursStatus);
            }).ToList();

            return new PlannerRowDto(pid, personNames[pid], cells);
        }).OrderBy(r => r.EmployeeName).ToList();

        return Results.Ok(rows);
    }

    private static async Task<IResult> PlannerAssign(
        PlannerAssignRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        if (req.AssignType == "absence")
        {
            var absType = req.AbsenceType?.ToLowerInvariant() switch
            {
                "ferie" => AbsenceType.Ferie,
                "avspasering" => AbsenceType.Avspasering,
                "sykmelding" => AbsenceType.Sykmelding,
                "egenmelding" => AbsenceType.Egenmelding,
                _ => AbsenceType.Annet
            };

            // Calculate hours (business days × 7.5)
            var hours = 0m;
            for (var d = req.FromDate; d <= req.ToDate; d = d.AddDays(1))
                if (d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) hours += 7.5m;

            var absence = new Absence
            {
                TenantId = tp.TenantId.Value,
                PersonId = req.PersonId,
                Type = absType,
                StartDate = req.FromDate,
                EndDate = req.ToDate,
                Hours = hours,
                Status = AbsenceStatus.Approved, // Admin-created = auto-approved
                Notes = "Registrert via planlegger"
            };

            // Handle avspasering bank deduction
            if (absType == AbsenceType.Avspasering && hours > 0)
            {
                var bankBalance = await db.OvertimeBankEntries
                    .Where(o => o.PersonId == req.PersonId && o.TenantId == tp.TenantId.Value)
                    .SumAsync(o => o.Hours, ct);
                if (bankBalance < hours)
                    return Results.BadRequest(new { error = $"Ikke nok i timebanken. Saldo: {bankBalance:0.#}t" });

                db.OvertimeBankEntries.Add(new OvertimeBankEntry
                {
                    TenantId = tp.TenantId.Value,
                    PersonId = req.PersonId,
                    Date = req.FromDate,
                    Hours = -hours,
                    Action = OvertimeBankAction.UsedAsTimeOff,
                    Description = $"Avspasering {req.FromDate:dd.MM}–{req.ToDate:dd.MM} (planlegger)"
                });
            }

            db.Absences.Add(absence);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { type = "absence", days = (int)(req.ToDate.DayNumber - req.FromDate.DayNumber + 1) });
        }

        // "project" type — update project/job on existing entries without changing shift
        if (req.AssignType == "project")
        {
            var existing = await db.PlannerEntries
                .Where(p => p.PersonId == req.PersonId && p.TenantId == tp.TenantId.Value
                    && p.Date >= req.FromDate && p.Date <= req.ToDate)
                .ToListAsync(ct);

            if (existing.Count == 0)
            {
                // No entries exist — create blank ones with just project/job
                for (var d = req.FromDate; d <= req.ToDate; d = d.AddDays(1))
                {
                    // Need a default shift — use first active shift or skip
                    var defaultShift = await db.ShiftDefinitions
                        .Where(s => s.TenantId == tp.TenantId.Value && s.IsActive && s.IsWorkDay)
                        .Select(s => s.Id).FirstOrDefaultAsync(ct);
                    if (defaultShift == Guid.Empty) continue;

                    db.PlannerEntries.Add(new PlannerEntry
                    {
                        TenantId = tp.TenantId.Value,
                        PersonId = req.PersonId,
                        Date = d,
                        ShiftDefinitionId = defaultShift,
                        ProjectId = req.ProjectId,
                        JobId = req.JobId
                    });
                }
            }
            else
            {
                foreach (var entry in existing)
                {
                    entry.ProjectId = req.ProjectId;
                    entry.JobId = req.JobId;
                }
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { type = "project", days = (int)(req.ToDate.DayNumber - req.FromDate.DayNumber + 1) });
        }

        if (!req.ShiftDefinitionId.HasValue)
            return Results.BadRequest(new { error = "ShiftDefinitionId er påkrevd for skifttildeling." });

        // Remove existing planner entries for this person in the range
        var existingEntries = await db.PlannerEntries
            .Where(p => p.PersonId == req.PersonId && p.TenantId == tp.TenantId.Value
                && p.Date >= req.FromDate && p.Date <= req.ToDate)
            .ToListAsync(ct);
        db.PlannerEntries.RemoveRange(existingEntries);

        // Create new entries for each day in range
        var count = 0;
        for (var d = req.FromDate; d <= req.ToDate; d = d.AddDays(1))
        {
            db.PlannerEntries.Add(new PlannerEntry
            {
                TenantId = tp.TenantId.Value,
                PersonId = req.PersonId,
                Date = d,
                ShiftDefinitionId = req.ShiftDefinitionId.Value,
                ProjectId = req.ProjectId,
                JobId = req.JobId
            });
            count++;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { type = "shift", days = count });
    }

    // ─── Planner Clear ────────────────────────────────────────────

    private static async Task<IResult> PlannerClear(
        PlannerAssignRequest req,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        // Delete planner entries in range
        var entries = await db.PlannerEntries
            .Where(p => p.PersonId == req.PersonId && p.TenantId == tp.TenantId.Value
                && p.Date >= req.FromDate && p.Date <= req.ToDate)
            .ToListAsync(ct);
        db.PlannerEntries.RemoveRange(entries);

        // Also delete absences in range (admin action)
        var absences = await db.Absences
            .Where(a => a.PersonId == req.PersonId && a.TenantId == tp.TenantId.Value
                && a.StartDate >= req.FromDate && a.EndDate <= req.ToDate)
            .ToListAsync(ct);
        db.Absences.RemoveRange(absences);

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { cleared = entries.Count + absences.Count });
    }

    // ─── Employee Calendar ──────────────────────────────────────────

    private static async Task<IResult> GetEmployeeCalendar(
        Guid personId,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct,
        string? from = null,
        string? to = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;

        var fromDate = DateOnly.TryParse(from, out var f) ? f : new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);
        var toDate = DateOnly.TryParse(to, out var t2) ? t2 : fromDate.AddMonths(1).AddDays(-1);

        var empName = await db.Persons.Where(p => p.Id == personId).Select(p => p.FullName).FirstOrDefaultAsync(ct) ?? "";

        var plannerDays = await db.PlannerEntries
            .Where(p => p.TenantId == tenantId && p.PersonId == personId && p.Date >= fromDate && p.Date <= toDate)
            .Select(p => new { p.Date, p.ShiftDefinitionId, p.ProjectId, p.JobId })
            .ToListAsync(ct);

        var shiftIds = plannerDays.Select(p => p.ShiftDefinitionId).Distinct().ToList();
        var shifts = shiftIds.Count > 0
            ? await db.ShiftDefinitions.Where(s => shiftIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s, ct)
            : new Dictionary<Guid, ShiftDefinition>();

        var projIds = plannerDays.Where(p => p.ProjectId.HasValue).Select(p => p.ProjectId!.Value).Distinct().ToList();
        var projNames = projIds.Count > 0
            ? await db.Projects.Where(p => projIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct)
            : new Dictionary<Guid, string>();

        var jobIds = plannerDays.Where(p => p.JobId.HasValue).Select(p => p.JobId!.Value).Distinct().ToList();
        var jobDescs = jobIds.Count > 0
            ? await db.Jobs.Where(j => jobIds.Contains(j.Id)).ToDictionaryAsync(j => j.Id, j => j.Description ?? "", ct)
            : new Dictionary<Guid, string>();

        var hours = await db.TimeEntries
            .Where(e => e.PersonId == personId && e.Date >= fromDate && e.Date <= toDate)
            .Select(e => new { e.Date, e.Hours, e.Status })
            .ToListAsync(ct);

        var absences = await db.Absences
            .Where(a => a.PersonId == personId && a.StartDate <= toDate && a.EndDate >= fromDate
                && a.Status != AbsenceStatus.Rejected)
            .Select(a => new { a.Type, a.StartDate, a.EndDate })
            .ToListAsync(ct);

        var dates = new List<DateOnly>();
        for (var d = fromDate; d <= toDate; d = d.AddDays(1)) dates.Add(d);

        var days = dates.Select(date =>
        {
            var planner = plannerDays.FirstOrDefault(p => p.Date == date);
            var absence = absences.FirstOrDefault(a => a.StartDate <= date && a.EndDate >= date);
            var dayHours = hours.Where(h => h.Date == date).ToList();

            string? shiftName = null, shiftColor = null, projName = null, absType = null;
            if (planner is not null && shifts.TryGetValue(planner.ShiftDefinitionId, out var shift))
            {
                shiftName = shift.Name; shiftColor = shift.Color;
                if (planner.ProjectId.HasValue && projNames.TryGetValue(planner.ProjectId.Value, out var pn))
                    projName = pn;
                else if (planner.JobId.HasValue && jobDescs.TryGetValue(planner.JobId.Value, out var jd))
                    projName = jd;
            }
            if (absence is not null)
            {
                absType = absence.Type.ToString();
                // Absence takes priority — hide shift for this day
                shiftName = null;
                shiftColor = null;
                projName = null;
            }

            var totalHours = dayHours.Sum(h => h.Hours);
            var status = absence is not null ? "Fravær"
                : dayHours.Count == 0 ? ""
                : dayHours.All(h => h.Status == TimeEntryStatus.Approved) ? "Godkjent"
                : "Registrert";

            return new EmployeeCalendarDayDto(date, shiftName, shiftColor, projName, absType, totalHours, status);
        }).ToList();

        return Results.Ok(new EmployeeCalendarDto(empName, days));
    }

    // ─── Hours Settings ─────────────────────────────────────────────

    private static async Task<IResult> GetHoursSettings(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

        return Results.Ok(new HoursSettingsDto(tenant.TimebankEnabled, tenant.OvertimeStackingMode));
    }

    private static async Task<IResult> UpdateHoursSettings(
        UpdateHoursSettingsRequest req, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tp.TenantId.Value, ct);
        if (tenant is null) return Results.NotFound();

        tenant.TimebankEnabled = req.TimebankEnabled;
        tenant.OvertimeStackingMode = req.OvertimeStackingMode;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }
}
