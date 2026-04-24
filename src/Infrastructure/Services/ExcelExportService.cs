using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Services;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.Infrastructure.Services;

public class ExcelExportService(SolodocDbContext db) : IExcelExportService
{
    public async Task<byte[]> GeneratePayrollExportAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var entries = await db.TimeEntries
            .Where(t => t.TenantId == tenantId && t.Date >= from && t.Date <= to)
            .GroupBy(t => t.PersonId)
            .Select(g => new
            {
                PersonId = g.Key,
                Hours = g.Sum(t => t.Hours),
                OvertimeHours = g.Sum(t => t.OvertimeHours)
            })
            .ToListAsync(ct);

        var personIds = entries.Select(e => e.PersonId).ToList();
        var persons = await db.Persons.Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        var expenses = await db.Expenses
            .Where(e => e.TenantId == tenantId && e.Date >= from && e.Date <= to
                && (e.Status == ExpenseStatus.Approved || e.Status == ExpenseStatus.Paid))
            .GroupBy(e => e.PersonId)
            .Select(g => new { PersonId = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(g => g.PersonId, g => g.Total, ct);

        var travelExpenses = await db.TravelExpenses
            .Where(t => t.TenantId == tenantId && t.DepartureDate >= from && t.ReturnDate <= to
                && (t.Status == ExpenseStatus.Approved || t.Status == ExpenseStatus.Paid))
            .GroupBy(t => t.PersonId)
            .Select(g => new { PersonId = g.Key, Total = g.Sum(t => t.TotalAmount) })
            .ToDictionaryAsync(g => g.PersonId, g => g.Total, ct);

        var absences = await db.Absences
            .Where(a => a.TenantId == tenantId && a.StartDate <= to && a.EndDate >= from)
            .ToListAsync(ct);

        // Allowances
        var allowances = await db.TimeEntryAllowances
            .Where(a => db.TimeEntries.Any(t => t.Id == a.TimeEntryId && t.TenantId == tenantId && t.Date >= from && t.Date <= to))
            .GroupBy(a => db.TimeEntries.Where(t => t.Id == a.TimeEntryId).Select(t => t.PersonId).FirstOrDefault())
            .Select(g => new { PersonId = g.Key, Total = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(g => g.PersonId, g => g.Total, ct);

        // Tenant name for header
        var tenantName = await db.Tenants.Where(t => t.Id == tenantId).Select(t => t.Name).FirstOrDefaultAsync(ct) ?? "";

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Lønnseksport");

        // Company header
        ws.Cell(1, 1).Value = tenantName;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(2, 1).Value = "Lønnseksport";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 12;
        ws.Cell(3, 1).Value = $"Periode: {from:dd.MM.yyyy} – {to:dd.MM.yyyy}";
        ws.Cell(3, 1).Style.Font.Italic = true;
        ws.Cell(4, 1).Value = $"Generert: {DateTimeOffset.UtcNow:dd.MM.yyyy HH:mm}";
        ws.Cell(4, 1).Style.Font.FontColor = XLColor.Gray;

        // Column headers
        var headers = new[] { "Ansatt", "Timer", "Overtid", "Tillegg", "Utlegg", "Reise", "Ferie (dager)", "Sykdom (dager)", "Total" };
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(6, i + 1).Value = headers[i];
            ws.Cell(6, i + 1).Style.Font.Bold = true;
            ws.Cell(6, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4361EE");
            ws.Cell(6, i + 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(6, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        var row = 7;
        foreach (var entry in entries.OrderBy(e => persons.GetValueOrDefault(e.PersonId, "")))
        {
            persons.TryGetValue(entry.PersonId, out var name);
            var allowance = allowances.GetValueOrDefault(entry.PersonId, 0m);
            var exp = expenses.GetValueOrDefault(entry.PersonId, 0m);
            var travel = travelExpenses.GetValueOrDefault(entry.PersonId, 0m);
            var vacation = absences.Where(a => a.PersonId == entry.PersonId && a.Type == AbsenceType.Ferie)
                .Sum(a => a.Hours / 7.5m);
            var sick = absences.Where(a => a.PersonId == entry.PersonId &&
                (a.Type == AbsenceType.Sykmelding || a.Type == AbsenceType.Egenmelding))
                .Sum(a => a.Hours / 7.5m);

            ws.Cell(row, 1).Value = name ?? "";
            ws.Cell(row, 2).Value = (double)entry.Hours;
            ws.Cell(row, 3).Value = (double)entry.OvertimeHours;
            ws.Cell(row, 4).Value = (double)allowance;
            ws.Cell(row, 5).Value = (double)exp;
            ws.Cell(row, 6).Value = (double)travel;
            ws.Cell(row, 7).Value = (double)vacation;
            ws.Cell(row, 8).Value = (double)sick;
            ws.Cell(row, 9).FormulaA1 = $"=SUM(B{row}:H{row})";

            // Zebra striping
            if (row % 2 == 0)
                ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");

            for (var c = 2; c <= 9; c++)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";

            row++;
        }

        // Totals row
        ws.Cell(row, 1).Value = "TOTALT";
        ws.Cell(row, 1).Style.Font.Bold = true;
        for (var c = 2; c <= 9; c++)
        {
            ws.Cell(row, c).FormulaA1 = $"=SUM({(char)('A' + c - 1)}7:{(char)('A' + c - 1)}{row - 1})";
            ws.Cell(row, c).Style.Font.Bold = true;
            ws.Cell(row, c).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, c).Style.Border.TopBorder = XLBorderStyleValues.Double;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> GenerateHoursExportAsync(Guid tenantId, DateOnly from, DateOnly to, Guid? projectId, Guid? personId, CancellationToken ct)
    {
        var query = db.TimeEntries.Where(t => t.TenantId == tenantId && t.Date >= from && t.Date <= to);
        if (projectId.HasValue) query = query.Where(t => t.ProjectId == projectId.Value);
        if (personId.HasValue) query = query.Where(t => t.PersonId == personId.Value);

        var entries = await query.OrderBy(t => t.Date).ThenBy(t => t.PersonId)
            .Select(t => new { t.PersonId, t.Date, t.Hours, t.OvertimeHours, t.Category, t.Status, t.ProjectId, t.Notes })
            .ToListAsync(ct);

        var pIds = entries.Select(e => e.PersonId).Distinct().ToList();
        var persons = await db.Persons.Where(p => pIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct);
        var projIds = entries.Where(e => e.ProjectId.HasValue).Select(e => e.ProjectId!.Value).Distinct().ToList();
        var projects = projIds.Count > 0 ? await db.Projects.Where(p => projIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct) : new();

        var tenantName = await db.Tenants.Where(t => t.Id == tenantId).Select(t => t.Name).FirstOrDefaultAsync(ct) ?? "";

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Timer");
        ws.Cell(1, 1).Value = tenantName;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        AddHeader(ws, "Timeeksport", $"{from:dd.MM.yyyy} – {to:dd.MM.yyyy}");

        var headers = new[] { "Ansatt", "Dato", "Timer", "Overtid", "Prosjekt", "Notat" };
        var headerRow = 5;
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(headerRow, i + 1).Value = headers[i];
            ws.Cell(headerRow, i + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4361EE");
            ws.Cell(headerRow, i + 1).Style.Font.FontColor = XLColor.White;
        }

        var row = 6;
        foreach (var e in entries)
        {
            persons.TryGetValue(e.PersonId, out var name);
            var proj = e.ProjectId.HasValue && projects.TryGetValue(e.ProjectId.Value, out var pn) ? pn : "";
            ws.Cell(row, 1).Value = name ?? "";
            ws.Cell(row, 2).Value = e.Date.ToString("dd.MM.yyyy");
            ws.Cell(row, 3).Value = (double)e.Hours;
            ws.Cell(row, 4).Value = (double)e.OvertimeHours;
            ws.Cell(row, 5).Value = proj;
            ws.Cell(row, 6).Value = e.Notes ?? "";
            if (row % 2 == 0)
                ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9FA");
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> GenerateExpenseExportAsync(Guid tenantId, DateOnly from, DateOnly to, string? status, CancellationToken ct)
    {
        var query = db.Expenses.Where(e => e.TenantId == tenantId && e.Date >= from && e.Date <= to);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ExpenseStatus>(status, true, out var s))
            query = query.Where(e => e.Status == s);

        var items = await query.OrderBy(e => e.Date)
            .Select(e => new { e.PersonId, e.Date, e.Amount, e.Category, e.Status, e.Description, e.ProjectId })
            .ToListAsync(ct);

        var pIds = items.Select(e => e.PersonId).Distinct().ToList();
        var persons = await db.Persons.Where(p => pIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Utlegg");
        AddHeader(ws, "Utleggseksport", $"{from:dd.MM.yyyy} – {to:dd.MM.yyyy}");

        AddColumnHeaders(ws, 4, ["Ansatt", "Dato", "Beløp", "Kategori", "Status", "Beskrivelse"]);

        var row = 5;
        foreach (var e in items)
        {
            persons.TryGetValue(e.PersonId, out var name);
            ws.Cell(row, 1).Value = name ?? "";
            ws.Cell(row, 2).Value = e.Date.ToString("dd.MM.yyyy");
            ws.Cell(row, 3).Value = (double)e.Amount;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Value = e.Category.ToString();
            ws.Cell(row, 5).Value = e.Status.ToString();
            ws.Cell(row, 6).Value = e.Description ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> GenerateDeviationExportAsync(Guid tenantId, DateOnly from, DateOnly to, string? severity, string? status, Guid? projectId, CancellationToken ct)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue);
        var toDt = to.ToDateTime(TimeOnly.MaxValue);
        var query = db.Deviations.Where(d => d.TenantId == tenantId && d.CreatedAt >= new DateTimeOffset(fromDt) && d.CreatedAt <= new DateTimeOffset(toDt));
        if (projectId.HasValue) query = query.Where(d => d.ProjectId == projectId.Value);

        var items = await query.OrderByDescending(d => d.CreatedAt)
            .Select(d => new { d.Title, d.Description, d.Severity, d.Status, d.CreatedAt, d.ProjectId, d.ReportedById })
            .ToListAsync(ct);

        var pIds = items.Select(d => d.ReportedById).Distinct().ToList();
        var persons = await db.Persons.Where(p => pIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Avvik");
        AddHeader(ws, "Avvikseksport", $"{from:dd.MM.yyyy} – {to:dd.MM.yyyy}");

        AddColumnHeaders(ws, 4, ["Tittel", "Beskrivelse", "Alvorlighet", "Status", "Rapportert av", "Dato"]);

        var row = 5;
        foreach (var d in items)
        {
            persons.TryGetValue(d.ReportedById, out var name);
            ws.Cell(row, 1).Value = d.Title;
            ws.Cell(row, 2).Value = d.Description ?? "";
            ws.Cell(row, 3).Value = d.Severity.ToString();
            ws.Cell(row, 4).Value = d.Status.ToString();
            ws.Cell(row, 5).Value = name ?? "";
            ws.Cell(row, 6).Value = d.CreatedAt.ToString("dd.MM.yyyy");
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> GenerateCertificationExportAsync(Guid tenantId, string? type, Guid? personId, string? status, CancellationToken ct)
    {
        var query = db.EmployeeCertifications.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrEmpty(type)) query = query.Where(c => c.Type == type);
        if (personId.HasValue) query = query.Where(c => c.PersonId == personId.Value);

        var items = await query.OrderBy(c => c.PersonId).ThenBy(c => c.Name)
            .Select(c => new { c.PersonId, c.Name, c.Type, c.IssuedBy, c.ExpiryDate })
            .ToListAsync(ct);

        var pIds = items.Select(c => c.PersonId).Distinct().ToList();
        var persons = await db.Persons.Where(p => pIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sertifikater");
        AddHeader(ws, "Sertifikateksport", "");

        AddColumnHeaders(ws, 4, ["Ansatt", "Sertifikat", "Type", "Utstedt av", "Utløpsdato", "Status"]);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var row = 5;
        foreach (var c in items)
        {
            persons.TryGetValue(c.PersonId, out var name);
            var expStatus = c.ExpiryDate.HasValue
                ? (c.ExpiryDate.Value < today ? "Utløpt" : c.ExpiryDate.Value < today.AddDays(30) ? "Utløper snart" : "Gyldig")
                : "Ingen dato";
            ws.Cell(row, 1).Value = name ?? "";
            ws.Cell(row, 2).Value = c.Name;
            ws.Cell(row, 3).Value = c.Type ?? "";
            ws.Cell(row, 4).Value = c.IssuedBy ?? "";
            ws.Cell(row, 5).Value = c.ExpiryDate?.ToString("dd.MM.yyyy") ?? "";
            ws.Cell(row, 6).Value = expStatus;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> GenerateEmployeeExportAsync(Guid tenantId, Guid personId, CancellationToken ct)
    {
        var person = await db.Persons.FirstOrDefaultAsync(p => p.Id == personId, ct);
        if (person is null) return [];

        using var wb = new XLWorkbook();

        // Sheet 1: Personal info
        var wsInfo = wb.Worksheets.Add("Ansattinfo");
        wsInfo.Cell(1, 1).Value = person.FullName;
        wsInfo.Cell(1, 1).Style.Font.Bold = true;
        wsInfo.Cell(1, 1).Style.Font.FontSize = 14;
        wsInfo.Cell(2, 1).Value = "E-post:"; wsInfo.Cell(2, 2).Value = person.Email;
        wsInfo.Cell(3, 1).Value = "Telefon:"; wsInfo.Cell(3, 2).Value = person.PhoneNumber ?? "";
        wsInfo.Columns().AdjustToContents();

        // Sheet 2: Certifications
        var certs = await db.EmployeeCertifications.Where(c => c.PersonId == personId && c.TenantId == tenantId)
            .OrderBy(c => c.Name).ToListAsync(ct);
        var wsCerts = wb.Worksheets.Add("Sertifikater");
        AddColumnHeaders(wsCerts, 1, ["Sertifikat", "Type", "Utstedt av", "Utløpsdato"]);
        var row = 2;
        foreach (var c in certs)
        {
            wsCerts.Cell(row, 1).Value = c.Name;
            wsCerts.Cell(row, 2).Value = c.Type ?? "";
            wsCerts.Cell(row, 3).Value = c.IssuedBy ?? "";
            wsCerts.Cell(row, 4).Value = c.ExpiryDate?.ToString("dd.MM.yyyy") ?? "";
            row++;
        }
        wsCerts.Columns().AdjustToContents();

        // Sheet 3: Hours summary (last 12 months)
        var from = DateOnly.FromDateTime(DateTime.Today.AddMonths(-12));
        var hours = await db.TimeEntries.Where(t => t.PersonId == personId && t.TenantId == tenantId && t.Date >= from)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Hours = g.Sum(t => t.Hours), Overtime = g.Sum(t => t.OvertimeHours) })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync(ct);
        var wsHours = wb.Worksheets.Add("Timer");
        AddColumnHeaders(wsHours, 1, ["Måned", "Timer", "Overtid"]);
        row = 2;
        foreach (var h in hours)
        {
            wsHours.Cell(row, 1).Value = $"{h.Year}-{h.Month:D2}";
            wsHours.Cell(row, 2).Value = (double)h.Hours;
            wsHours.Cell(row, 3).Value = (double)h.Overtime;
            row++;
        }
        wsHours.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── Shared helpers ──

    private static void AddHeader(IXLWorksheet ws, string title, string subtitle)
    {
        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        if (!string.IsNullOrEmpty(subtitle))
            ws.Cell(2, 1).Value = subtitle;
    }

    private static void AddColumnHeaders(IXLWorksheet ws, int row, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }
    }
}
