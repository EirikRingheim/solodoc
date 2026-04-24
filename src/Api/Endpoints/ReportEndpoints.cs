using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Application.Services;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Reports;

namespace Solodoc.Api.Endpoints;

public static class ReportEndpoints
{
    public static WebApplication MapReportEndpoints(this WebApplication app)
    {
        app.MapGet("/api/reports/hours", GetHoursReport).RequireAuthorization();
        app.MapGet("/api/reports/deviations", GetDeviationReport).RequireAuthorization();
        app.MapGet("/api/reports/certifications", GetCertificationReport).RequireAuthorization();
        app.MapGet("/api/reports/safety", GetSafetyReport).RequireAuthorization();
        app.MapGet("/api/reports/project/{id:guid}/summary", GetProjectSummary).RequireAuthorization();
        app.MapGet("/api/reports/accounting", GetAccountingReport).RequireAuthorization();

        // CSV Exports
        app.MapGet("/api/reports/hours/export", ExportHoursCsv).RequireAuthorization();
        app.MapGet("/api/reports/deviations/export", ExportDeviationsCsv).RequireAuthorization();
        app.MapGet("/api/reports/certifications/export", ExportCertificationsCsv).RequireAuthorization();
        app.MapGet("/api/reports/safety/export", ExportSafetyCsv).RequireAuthorization();
        app.MapGet("/api/reports/project/{id:guid}/personnel/export", ExportProjectPersonnelCsv).RequireAuthorization();

        // Payroll Excel export
        app.MapGet("/api/reports/payroll/export", async (
            IExcelExportService excelService, ITenantProvider tp, CancellationToken ct,
            string? from = null, string? to = null) =>
        {
            if (tp.TenantId is null) return Results.Unauthorized();
            var fromDate = DateOnly.TryParse(from, out var fd) ? fd : DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-30).UtcDateTime);
            var toDate = DateOnly.TryParse(to, out var td) ? td : DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            var bytes = await excelService.GeneratePayrollExportAsync(tp.TenantId.Value, fromDate, toDate, ct);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"lonnseksport-{fromDate:yyyy-MM-dd}.xlsx");
        }).RequireAuthorization();

        // Generic Excel exports
        app.MapGet("/api/reports/hours/export-excel", async (
            IExcelExportService excelService, ITenantProvider tp, CancellationToken ct,
            string? from = null, string? to = null, Guid? projectId = null, Guid? personId = null) =>
        {
            if (tp.TenantId is null) return Results.Unauthorized();
            var fromDate = DateOnly.TryParse(from, out var fd) ? fd : DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-30).UtcDateTime);
            var toDate = DateOnly.TryParse(to, out var td) ? td : DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            var bytes = await excelService.GenerateHoursExportAsync(tp.TenantId.Value, fromDate, toDate, projectId, personId, ct);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "timer.xlsx");
        }).RequireAuthorization();

        app.MapGet("/api/reports/deviations/export-excel", async (
            IExcelExportService excelService, ITenantProvider tp, CancellationToken ct,
            string? from = null, string? to = null, string? severity = null, string? status = null, Guid? projectId = null) =>
        {
            if (tp.TenantId is null) return Results.Unauthorized();
            var fromDate = DateOnly.TryParse(from, out var fd) ? fd : DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-90).UtcDateTime);
            var toDate = DateOnly.TryParse(to, out var td) ? td : DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
            var bytes = await excelService.GenerateDeviationExportAsync(tp.TenantId.Value, fromDate, toDate, severity, status, projectId, ct);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "avvik.xlsx");
        }).RequireAuthorization();

        app.MapGet("/api/reports/certifications/export-excel", async (
            IExcelExportService excelService, ITenantProvider tp, CancellationToken ct,
            string? type = null, Guid? personId = null, string? status = null) =>
        {
            if (tp.TenantId is null) return Results.Unauthorized();
            var bytes = await excelService.GenerateCertificationExportAsync(tp.TenantId.Value, type, personId, status, ct);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sertifikater.xlsx");
        }).RequireAuthorization();

        app.MapGet("/api/reports/employee/{personId:guid}/export-excel", async (
            Guid personId, IExcelExportService excelService, ITenantProvider tp, CancellationToken ct) =>
        {
            if (tp.TenantId is null) return Results.Unauthorized();
            var bytes = await excelService.GenerateEmployeeExportAsync(tp.TenantId.Value, personId, ct);
            return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ansatt.xlsx");
        }).RequireAuthorization();

        return app;
    }

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static string SeverityToString(DeviationSeverity s) => s switch
    {
        DeviationSeverity.Low => "Lav",
        DeviationSeverity.Medium => "Middels",
        DeviationSeverity.High => "Høy",
        _ => "Ukjent"
    };

    private static string CategoryToString(TimeEntryCategory cat) => cat switch
    {
        TimeEntryCategory.Arbeid => "Arbeid",
        TimeEntryCategory.Reise => "Reise",
        TimeEntryCategory.Kontorarbeid => "Kontorarbeid",
        TimeEntryCategory.Lagerarbeid => "Lagerarbeid",
        TimeEntryCategory.Kurs => "Kurs",
        TimeEntryCategory.Annet => "Annet",
        _ => "Arbeid"
    };

    // ─── Hours Report ───────────────────────────────────────────────

    private static async Task<IResult> GetHoursReport(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct,
        string? from = null,
        string? to = null,
        Guid? projectId = null,
        Guid? personId = null)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        var tenantId = tenantProvider.TenantId.Value;

        var fromDate = DateOnly.TryParse(from, out var fd)
            ? fd
            : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = DateOnly.TryParse(to, out var td)
            ? td
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var query = db.TimeEntries
            .Where(t => t.TenantId == tenantId && t.Date >= fromDate && t.Date <= toDate);

        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        if (personId.HasValue)
            query = query.Where(t => t.PersonId == personId.Value);

        var entries = await query.ToListAsync(ct);

        var totalHours = entries.Sum(e => e.Hours);
        var totalOvertime = entries.Sum(e => e.OvertimeHours);
        var dayCount = (toDate.ToDateTime(TimeOnly.MinValue) - fromDate.ToDateTime(TimeOnly.MinValue)).Days + 1;
        var avgPerDay = dayCount > 0 ? totalHours / dayCount : 0m;

        var byCategory = entries
            .GroupBy(e => CategoryToString(e.Category))
            .Select(g => new CategoryBreakdown(g.Key, g.Sum(e => e.Hours)))
            .OrderByDescending(c => c.Hours)
            .ToList();

        // Project breakdown — need project names
        var projectIds = entries.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).Distinct().ToList();
        var projectNames = await db.Projects
            .Where(p => projectIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var byProject = entries
            .Where(e => e.ProjectId.HasValue)
            .GroupBy(e => e.ProjectId!.Value)
            .Select(g => new ProjectBreakdown(
                projectNames.GetValueOrDefault(g.Key, "Ukjent prosjekt"),
                g.Sum(e => e.Hours),
                g.Sum(e => e.OvertimeHours)))
            .OrderByDescending(p => p.Hours)
            .ToList();

        var byDay = entries
            .GroupBy(e => e.Date)
            .Select(g => new DailyHours(g.Key, g.Sum(e => e.Hours), g.Sum(e => e.OvertimeHours)))
            .OrderBy(d => d.Date)
            .ToList();

        // Employee breakdown — need person names
        var personIds = entries.Select(e => e.PersonId).Distinct().ToList();
        var personNames = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FullName })
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var byEmployee = entries
            .GroupBy(e => e.PersonId)
            .Select(g => new EmployeeHours(
                personNames.GetValueOrDefault(g.Key, "Ukjent"),
                g.Sum(e => e.Hours),
                g.Sum(e => e.OvertimeHours)))
            .OrderByDescending(e => e.Hours)
            .ToList();

        return Results.Ok(new HoursReportDto(
            totalHours, totalOvertime, avgPerDay,
            byCategory, byProject, byDay, byEmployee));
    }

    // ─── Deviation Report ───────────────────────────────────────────

    private static async Task<IResult> GetDeviationReport(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct,
        string? from = null,
        string? to = null,
        Guid? projectId = null)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        var tenantId = tenantProvider.TenantId.Value;

        var fromDate = DateTimeOffset.TryParse(from, out var fd)
            ? fd
            : DateTimeOffset.UtcNow.AddDays(-90);
        var toDate = DateTimeOffset.TryParse(to, out var td)
            ? td
            : DateTimeOffset.UtcNow;

        var query = db.Deviations
            .Where(d => d.TenantId == tenantId && d.CreatedAt >= fromDate && d.CreatedAt <= toDate);

        if (projectId.HasValue)
            query = query.Where(d => d.ProjectId == projectId.Value);

        var deviations = await query.ToListAsync(ct);

        var total = deviations.Count;
        var open = deviations.Count(d => d.Status == DeviationStatus.Open);
        var inProgress = deviations.Count(d => d.Status == DeviationStatus.InProgress);
        var closed = deviations.Count(d => d.Status == DeviationStatus.Closed);

        var bySeverity = deviations
            .GroupBy(d => SeverityToString(d.Severity))
            .Select(g => new SeverityBreakdown(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .ToList();

        // By project — need project names
        var projIds = deviations.Where(d => d.ProjectId.HasValue).Select(d => d.ProjectId!.Value).Distinct().ToList();
        var projNames = await db.Projects
            .Where(p => projIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var byProject = deviations
            .Where(d => d.ProjectId.HasValue)
            .GroupBy(d => d.ProjectId!.Value)
            .Select(g => new ProjectDeviations(
                projNames.GetValueOrDefault(g.Key, "Ukjent prosjekt"),
                g.Count(d => d.Status == DeviationStatus.Open),
                g.Count(d => d.Status == DeviationStatus.InProgress),
                g.Count(d => d.Status == DeviationStatus.Closed)))
            .OrderByDescending(p => p.Open + p.InProgress)
            .ToList();

        var byMonth = deviations
            .GroupBy(d => d.CreatedAt.ToString("yyyy-MM"))
            .Select(g => new MonthlyDeviations(g.Key, g.Count()))
            .OrderBy(m => m.Month)
            .ToList();

        var closedDeviations = deviations
            .Where(d => d.Status == DeviationStatus.Closed && d.ClosedAt.HasValue)
            .ToList();
        var avgDaysToClose = closedDeviations.Count > 0
            ? (decimal)closedDeviations.Average(d => (d.ClosedAt!.Value - d.CreatedAt).TotalDays)
            : 0m;

        return Results.Ok(new DeviationReportDto(
            total, open, inProgress, closed,
            bySeverity, byProject, byMonth,
            Math.Round(avgDaysToClose, 1)));
    }

    // ─── Certification Report ───────────────────────────────────────

    private static async Task<IResult> GetCertificationReport(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        var tenantId = tenantProvider.TenantId.Value;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Get all employees in this tenant
        var memberPersonIds = await db.TenantMemberships
            .Where(m => m.TenantId == tenantId && m.State == TenantMembershipState.Active)
            .Select(m => m.PersonId)
            .ToListAsync(ct);

        var certs = await db.EmployeeCertifications
            .Where(c => memberPersonIds.Contains(c.PersonId))
            .ToListAsync(ct);

        var totalCerts = certs.Count;
        var expired = certs.Count(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < today);
        var expiring30 = certs.Count(c =>
            c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(30));
        var expiring60 = certs.Count(c =>
            c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(60));
        var expiring90 = certs.Count(c =>
            c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(90));

        var byType = certs
            .GroupBy(c => c.Type ?? "Ukjent")
            .Select(g => new CertTypeBreakdown(
                g.Key,
                g.Count(),
                g.Count(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < today),
                g.Count(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(90))))
            .OrderByDescending(t => t.Expired)
            .ToList();

        // Person names
        var personNames = await db.Persons
            .Where(p => memberPersonIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FullName })
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var employeeStatuses = certs
            .GroupBy(c => c.PersonId)
            .Select(g => new EmployeeCertStatus(
                personNames.GetValueOrDefault(g.Key, "Ukjent"),
                g.Count(),
                g.Count(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value < today),
                g.Count(c => c.ExpiryDate.HasValue && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(90))))
            .OrderByDescending(e => e.Expired)
            .ThenByDescending(e => e.ExpiringSoon)
            .ToList();

        return Results.Ok(new CertificationReportDto(
            totalCerts, expired, expiring30, expiring60, expiring90,
            byType, employeeStatuses));
    }

    // ─── Safety Report ──────────────────────────────────────────────

    private static async Task<IResult> GetSafetyReport(
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct,
        string? from = null,
        string? to = null)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();
        var tenantId = tenantProvider.TenantId.Value;

        var fromDate = DateTimeOffset.TryParse(from, out var fd)
            ? fd
            : DateTimeOffset.UtcNow.AddDays(-90);
        var toDate = DateTimeOffset.TryParse(to, out var td)
            ? td
            : DateTimeOffset.UtcNow;

        var sjaCount = await db.SjaForms
            .CountAsync(s => s.TenantId == tenantId && s.CreatedAt >= fromDate && s.CreatedAt <= toDate, ct);

        var safetyRoundsCompleted = await db.SafetyRoundSchedules
            .CountAsync(s => s.TenantId == tenantId && s.CreatedAt >= fromDate && s.CreatedAt <= toDate, ct);

        var hmsMeetingsHeld = await db.HmsMeetings
            .CountAsync(m => m.TenantId == tenantId && m.CreatedAt >= fromDate && m.CreatedAt <= toDate, ct);

        // Incident reports = deviations of type Personskade or Nestenulykke
        var incidentReports = await db.Deviations
            .CountAsync(d => d.TenantId == tenantId
                && d.CreatedAt >= fromDate && d.CreatedAt <= toDate
                && d.Type.HasValue
                && (d.Type.Value == DeviationType.Personskade || d.Type.Value == DeviationType.Nestenulykke), ct);

        var deviationsByMonth = await db.Deviations
            .Where(d => d.TenantId == tenantId && d.CreatedAt >= fromDate && d.CreatedAt <= toDate)
            .GroupBy(d => new { d.CreatedAt.Year, d.CreatedAt.Month })
            .Select(g => new MonthlyDeviations(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Count()))
            .OrderBy(m => m.Month)
            .ToListAsync(ct);

        return Results.Ok(new SafetyReportDto(
            sjaCount, safetyRoundsCompleted, hmsMeetingsHeld, incidentReports,
            deviationsByMonth));
    }

    // ─── Project Summary ────────────────────────────────────────────

    private static async Task<IResult> GetProjectSummary(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);
        if (project is null) return Results.NotFound();

        var statusStr = project.Status switch
        {
            ProjectStatus.Active => "Aktiv",
            ProjectStatus.Completed => "Fullført",
            _ => "Arkivert"
        };

        var totalHours = await db.TimeEntries
            .Where(t => t.ProjectId == id)
            .SumAsync(t => t.Hours, ct);

        var totalDeviations = await db.Deviations.CountAsync(d => d.ProjectId == id, ct);
        var openDeviations = await db.Deviations
            .CountAsync(d => d.ProjectId == id && d.Status != DeviationStatus.Closed, ct);

        var checklistsTotal = await db.ChecklistInstances.CountAsync(c => c.ProjectId == id, ct);
        var checklistsCompleted = await db.ChecklistInstances
            .CountAsync(c => c.ProjectId == id && (c.Status == ChecklistInstanceStatus.Submitted || c.Status == ChecklistInstanceStatus.Approved), ct);

        // Crew = distinct persons who have time entries on this project
        var crewCount = await db.TimeEntries
            .Where(t => t.ProjectId == id)
            .Select(t => t.PersonId)
            .Distinct()
            .CountAsync(ct);

        return Results.Ok(new ProjectReportSummaryDto(
            project.Name,
            statusStr,
            totalHours,
            totalDeviations,
            openDeviations,
            checklistsCompleted,
            checklistsTotal,
            crewCount,
            project.StartDate,
            project.PlannedEndDate));
    }

    // ── Accounting Report ───────────────────────────────

    private static async Task<IResult> GetAccountingReport(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        string? from = null, string? to = null, string? periodType = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;

        var periodStart = DateOnly.TryParse(from, out var f) ? f : new DateOnly(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, 1);
        var periodEnd = DateOnly.TryParse(to, out var t) ? t : periodStart.AddMonths(1).AddDays(-1);
        var pType = periodType ?? "Månedlig";

        // Hours
        var hours = await db.TimeEntries
            .Where(te => te.TenantId == tenantId && !te.IsDeleted && te.Date >= periodStart && te.Date <= periodEnd)
            .GroupBy(te => te.PersonId)
            .Select(g => new { PersonId = g.Key, Hours = g.Sum(te => te.Hours), Overtime = g.Sum(te => te.OvertimeHours) })
            .ToListAsync(ct);

        // Expenses
        var expenses = await db.Expenses
            .Where(e => e.TenantId == tenantId && !e.IsDeleted && e.Date >= periodStart && e.Date <= periodEnd
                && (e.Status == ExpenseStatus.Approved || e.Status == ExpenseStatus.Paid))
            .GroupBy(e => e.PersonId)
            .Select(g => new { PersonId = g.Key, Amount = g.Sum(e => e.Amount) })
            .ToListAsync(ct);

        // Travel expenses
        var travel = await db.TravelExpenses
            .Where(te => te.TenantId == tenantId && !te.IsDeleted && te.DepartureDate >= periodStart && te.DepartureDate <= periodEnd
                && (te.Status == ExpenseStatus.Approved || te.Status == ExpenseStatus.Paid))
            .GroupBy(te => te.PersonId)
            .Select(g => new { PersonId = g.Key, Amount = g.Sum(te => te.TotalAmount) })
            .ToListAsync(ct);

        // Absences
        var absences = await db.Absences
            .Where(a => a.TenantId == tenantId && !a.IsDeleted && a.StartDate <= periodEnd && a.EndDate >= periodStart)
            .ToListAsync(ct);

        var vacationDays = absences.Where(a => a.Type == AbsenceType.Ferie).Sum(a => (a.EndDate.ToDateTime(TimeOnly.MinValue) - a.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1);
        var sickDays = absences.Where(a => a.Type is AbsenceType.Sykmelding or AbsenceType.Egenmelding).Sum(a => (a.EndDate.ToDateTime(TimeOnly.MinValue) - a.StartDate.ToDateTime(TimeOnly.MinValue)).Days + 1);

        // Allowances
        var allowances = await db.TimeEntryAllowances
            .Where(a => !a.IsDeleted && a.TimeEntry.TenantId == tenantId && a.TimeEntry.Date >= periodStart && a.TimeEntry.Date <= periodEnd)
            .GroupBy(a => a.TimeEntry.PersonId)
            .Select(g => new { PersonId = g.Key, Amount = g.Sum(a => a.Amount) })
            .ToListAsync(ct);

        // Build per-employee rows
        var personIds = hours.Select(h => h.PersonId)
            .Union(expenses.Select(e => e.PersonId))
            .Union(travel.Select(t => t.PersonId))
            .Distinct().ToList();

        var personNames = await db.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var byEmployee = personIds.Select(pid =>
        {
            var h = hours.FirstOrDefault(x => x.PersonId == pid);
            var e = expenses.FirstOrDefault(x => x.PersonId == pid);
            var tr = travel.FirstOrDefault(x => x.PersonId == pid);
            var al = allowances.FirstOrDefault(x => x.PersonId == pid);
            personNames.TryGetValue(pid, out var name);
            var empHours = h?.Hours ?? 0;
            var empOt = h?.Overtime ?? 0;
            var empExp = e?.Amount ?? 0;
            var empTravel = tr?.Amount ?? 0;
            var empAllow = al?.Amount ?? 0;
            return new EmployeeAccountingRow(pid, name ?? "", empHours, empOt, empExp, empTravel, empAllow, 0, 0,
                empExp + empTravel + empAllow);
        }).OrderBy(e => e.EmployeeName).ToList();

        // By project
        var projectHours = await db.TimeEntries
            .Where(te => te.TenantId == tenantId && !te.IsDeleted && te.Date >= periodStart && te.Date <= periodEnd && te.ProjectId != null)
            .GroupBy(te => te.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, Hours = g.Sum(te => te.Hours), Overtime = g.Sum(te => te.OvertimeHours) })
            .ToListAsync(ct);

        var projectExpenses = await db.Expenses
            .Where(e => e.TenantId == tenantId && !e.IsDeleted && e.Date >= periodStart && e.Date <= periodEnd && e.ProjectId != null
                && (e.Status == ExpenseStatus.Approved || e.Status == ExpenseStatus.Paid))
            .GroupBy(e => e.ProjectId!.Value)
            .Select(g => new { ProjectId = g.Key, Amount = g.Sum(e => e.Amount) })
            .ToListAsync(ct);

        var projectIds = projectHours.Select(p => p.ProjectId).Union(projectExpenses.Select(p => p.ProjectId)).Distinct().ToList();
        var projectNames = await db.Projects.Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var byProject = projectIds.Select(pid =>
        {
            var h = projectHours.FirstOrDefault(x => x.ProjectId == pid);
            var e = projectExpenses.FirstOrDefault(x => x.ProjectId == pid);
            projectNames.TryGetValue(pid, out var name);
            return new ProjectAccountingRow(pid, name ?? "", h?.Hours ?? 0, h?.Overtime ?? 0, e?.Amount ?? 0, 0, 0,
                (e?.Amount ?? 0));
        }).OrderBy(p => p.ProjectName).ToList();

        var totals = new AccountingTotals(
            byEmployee.Sum(e => e.Hours), byEmployee.Sum(e => e.OvertimeHours),
            byEmployee.Sum(e => e.ExpenseAmount), byEmployee.Sum(e => e.TravelExpenseAmount),
            byEmployee.Sum(e => e.AllowanceAmount),
            vacationDays, sickDays,
            byEmployee.Sum(e => e.EmployeeTotal));

        return Results.Ok(new AccountingReportDto(periodStart, periodEnd, pType, totals, byEmployee, byProject));
    }

    // ─── CSV Exports ────────────────────────────────────────────────

    private static byte[] ToCsvBytes(System.Text.StringBuilder csv)
    {
        return System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    private static async Task<IResult> ExportHoursCsv(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        string? from = null, string? to = null, Guid? projectId = null, Guid? personId = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;
        var fromDate = DateOnly.TryParse(from, out var fd) ? fd : DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-30).UtcDateTime);
        var toDate = DateOnly.TryParse(to, out var td) ? td : DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        var query = db.TimeEntries.Where(t => t.TenantId == tenantId && t.Date >= fromDate && t.Date <= toDate);
        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
        if (personId.HasValue) query = query.Where(t => t.PersonId == personId.Value);

        var entries = await query.OrderBy(t => t.Date).ThenBy(t => t.PersonId)
            .Select(t => new { t.PersonId, t.Date, t.Hours, t.OvertimeHours, t.Category, t.Status, t.ProjectId, t.JobId, t.Notes })
            .ToListAsync(ct);

        var personIds = entries.Select(e => e.PersonId).Distinct().ToList();
        var persons = await db.Persons.Where(p => personIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct);
        var projIds = entries.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).Distinct().ToList();
        var projects = projIds.Count > 0 ? await db.Projects.Where(p => projIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct) : new();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Ansatt,Dato,Timer,Overtid,Kategori,Status,Prosjekt,Notat");
        foreach (var e in entries)
        {
            persons.TryGetValue(e.PersonId, out var name);
            var proj = e.ProjectId.HasValue && projects.TryGetValue(e.ProjectId.Value, out var pn) ? pn : "";
            csv.AppendLine($"\"{name}\",{e.Date:dd.MM.yyyy},{e.Hours:N2},{e.OvertimeHours:N2},{CategoryToString(e.Category)},{e.Status},\"{proj}\",\"{e.Notes ?? ""}\"");
        }
        return Results.File(ToCsvBytes(csv), "text/csv; charset=utf-8", $"timer-{fromDate:yyyy-MM-dd}-{toDate:yyyy-MM-dd}.csv");
    }

    private static async Task<IResult> ExportDeviationsCsv(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        string? from = null, string? to = null, string? severity = null, string? status = null, Guid? projectId = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;
        var fromDate = DateOnly.TryParse(from, out var fd) ? new DateTimeOffset(fd.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : DateTimeOffset.UtcNow.AddDays(-90);
        var toDate = DateOnly.TryParse(to, out var td) ? new DateTimeOffset(td.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero) : DateTimeOffset.UtcNow;

        var query = db.Deviations.Where(d => d.TenantId == tenantId && d.CreatedAt >= fromDate && d.CreatedAt <= toDate);
        if (projectId.HasValue) query = query.Where(d => d.ProjectId == projectId.Value);
        if (!string.IsNullOrEmpty(severity))
        {
            var sev = severity switch { "Lav" => DeviationSeverity.Low, "Middels" => DeviationSeverity.Medium, "Hoy" or "Høy" => DeviationSeverity.High, _ => (DeviationSeverity?)null };
            if (sev.HasValue) query = query.Where(d => d.Severity == sev.Value);
        }
        if (!string.IsNullOrEmpty(status))
        {
            var st = status switch { "Apen" or "Åpen" => DeviationStatus.Open, "UnderBehandling" => DeviationStatus.InProgress, "Lukket" => DeviationStatus.Closed, _ => (DeviationStatus?)null };
            if (st.HasValue) query = query.Where(d => d.Status == st.Value);
        }

        var deviations = await query.OrderByDescending(d => d.CreatedAt)
            .Select(d => new { d.Id, d.Title, d.Description, d.Status, d.Severity, d.CreatedAt, d.ClosedAt, d.ReportedById, d.ProjectId })
            .ToListAsync(ct);

        var personIds = deviations.Select(d => d.ReportedById).Distinct().ToList();
        var persons = await db.Persons.Where(p => personIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct);
        var projIds = deviations.Where(d => d.ProjectId.HasValue).Select(d => d.ProjectId!.Value).Distinct().ToList();
        var projects = projIds.Count > 0 ? await db.Projects.Where(p => projIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct) : new();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Tittel,Beskrivelse,Alvorlighet,Status,Prosjekt,Rapportert av,Opprettet,Lukket");
        foreach (var d in deviations)
        {
            persons.TryGetValue(d.ReportedById, out var reporter);
            var proj = d.ProjectId.HasValue && projects.TryGetValue(d.ProjectId.Value, out var pn) ? pn : "";
            var statusStr = d.Status switch { DeviationStatus.Open => "Apen", DeviationStatus.InProgress => "Under behandling", DeviationStatus.Closed => "Lukket", _ => "" };
            csv.AppendLine($"\"{d.Title}\",\"{d.Description ?? ""}\",{SeverityToString(d.Severity)},{statusStr},\"{proj}\",\"{reporter}\",{d.CreatedAt.ToOffset(TimeSpan.FromHours(2)):dd.MM.yyyy HH:mm},{d.ClosedAt?.ToOffset(TimeSpan.FromHours(2)).ToString("dd.MM.yyyy HH:mm") ?? ""}");
        }
        return Results.File(ToCsvBytes(csv), "text/csv; charset=utf-8", $"avvik-eksport.csv");
    }

    private static async Task<IResult> ExportCertificationsCsv(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        string? type = null, Guid? personId = null, string? status = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        var query = db.EmployeeCertifications.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrEmpty(type)) query = query.Where(c => c.Type == type);
        if (personId.HasValue) query = query.Where(c => c.PersonId == personId.Value);
        if (status == "expired") query = query.Where(c => c.ExpiryDate != null && c.ExpiryDate.Value < today);
        else if (status == "expiring") query = query.Where(c => c.ExpiryDate != null && c.ExpiryDate.Value >= today && c.ExpiryDate.Value <= today.AddDays(90));
        else if (status == "valid") query = query.Where(c => c.ExpiryDate == null || c.ExpiryDate.Value >= today);

        var certs = await query.OrderBy(c => c.PersonId).ThenBy(c => c.Type)
            .Select(c => new { c.PersonId, c.Name, c.Type, c.IssuedBy, c.ExpiryDate })
            .ToListAsync(ct);

        var personIds = certs.Select(c => c.PersonId).Distinct().ToList();
        var persons = await db.Persons.Where(p => personIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Ansatt,Sertifikat,Type,Utstedt av,Utlopsdato,Status");
        foreach (var c in certs)
        {
            persons.TryGetValue(c.PersonId, out var name);
            var expStatus = c.ExpiryDate is null ? "Ingen utlop" : c.ExpiryDate.Value < today ? "Utlopt" : c.ExpiryDate.Value <= today.AddDays(90) ? "Utloper snart" : "Gyldig";
            csv.AppendLine($"\"{name}\",\"{c.Name}\",\"{c.Type}\",\"{c.IssuedBy ?? ""}\",{c.ExpiryDate?.ToString("dd.MM.yyyy") ?? ""},\"{expStatus}\"");
        }
        return Results.File(ToCsvBytes(csv), "text/csv; charset=utf-8", $"sertifikater-eksport.csv");
    }

    private static async Task<IResult> ExportSafetyCsv(
        SolodocDbContext db, ITenantProvider tp, CancellationToken ct,
        string? from = null, string? to = null, string? type = null)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;
        var fromDate = DateOnly.TryParse(from, out var fd) ? fd : DateOnly.FromDateTime(DateTimeOffset.UtcNow.AddDays(-90).UtcDateTime);
        var toDate = DateOnly.TryParse(to, out var td) ? td : DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        // SJA forms
        var sjas = await db.SjaForms
            .Where(s => s.TenantId == tenantId && s.Date >= fromDate && s.Date <= toDate)
            .Select(s => new { s.Title, s.Status, s.Date, s.Location,
                Participants = db.SjaParticipants.Count(p => p.SjaFormId == s.Id),
                Hazards = db.SjaHazards.Count(h => h.SjaFormId == s.Id),
                ProjectName = s.ProjectId != null ? db.Projects.Where(p => p.Id == s.ProjectId).Select(p => p.Name).FirstOrDefault() : null })
            .ToListAsync(ct);

        var meetings = await db.HmsMeetings
            .Where(m => m.TenantId == tenantId && m.Date >= fromDate && m.Date <= toDate)
            .Select(m => new { m.Title, m.Date, m.Location })
            .ToListAsync(ct);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Type,Tittel,Dato,Sted,Prosjekt,Status,Deltakere,Farer");
        var includeAll = string.IsNullOrEmpty(type);
        if (includeAll || type == "SJA")
            foreach (var s in sjas)
                csv.AppendLine($"SJA,\"{s.Title}\",{s.Date:dd.MM.yyyy},\"{s.Location ?? ""}\",\"{s.ProjectName ?? ""}\",{s.Status},{s.Participants},{s.Hazards}");
        if (includeAll || type == "HMS-mote")
            foreach (var m in meetings)
                csv.AppendLine($"HMS-mote,\"{m.Title}\",{m.Date:dd.MM.yyyy},\"{m.Location ?? ""}\",,,");

        return Results.File(ToCsvBytes(csv), "text/csv; charset=utf-8", $"hms-eksport.csv");
    }

    private static async Task<IResult> ExportProjectPersonnelCsv(
        Guid id, SolodocDbContext db, ITenantProvider tp, CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tp.TenantId.Value, ct);
        if (project is null) return Results.NotFound();

        var entries = await db.TimeEntries
            .Where(t => t.ProjectId == id)
            .GroupBy(t => t.PersonId)
            .Select(g => new { PersonId = g.Key, Hours = g.Sum(t => t.Hours), OvertimeHours = g.Sum(t => t.OvertimeHours), Days = g.Select(t => t.Date).Distinct().Count() })
            .ToListAsync(ct);

        var personIds = entries.Select(e => e.PersonId).ToList();
        var persons = await db.Persons.Where(p => personIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Ansatt,Timer,Overtid,Antall dager");
        foreach (var e in entries.OrderByDescending(e => e.Hours))
        {
            persons.TryGetValue(e.PersonId, out var name);
            csv.AppendLine($"\"{name}\",{e.Hours:N2},{e.OvertimeHours:N2},{e.Days}");
        }
        return Results.File(ToCsvBytes(csv), "text/csv; charset=utf-8", $"personell-{project.Name}.csv");
    }
}
