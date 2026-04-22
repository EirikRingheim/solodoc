using System.Net.Http.Json;
using Solodoc.Shared.Reports;

namespace Solodoc.Client.Services;

public class ReportService(ApiHttpClient api)
{
    public async Task<HoursReportDto?> GetHoursReportAsync(
        DateOnly? from = null, DateOnly? to = null,
        Guid? projectId = null, Guid? personId = null)
    {
        var url = "api/reports/hours?";
        if (from.HasValue) url += $"from={from.Value:yyyy-MM-dd}&";
        if (to.HasValue) url += $"to={to.Value:yyyy-MM-dd}&";
        if (projectId.HasValue) url += $"projectId={projectId.Value}&";
        if (personId.HasValue) url += $"personId={personId.Value}&";

        var response = await api.GetAsync(url.TrimEnd('&', '?'));
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<HoursReportDto>();
        return null;
    }

    public async Task<DeviationReportDto?> GetDeviationReportAsync(
        DateOnly? from = null, DateOnly? to = null, Guid? projectId = null)
    {
        var url = "api/reports/deviations?";
        if (from.HasValue) url += $"from={from.Value:yyyy-MM-dd}&";
        if (to.HasValue) url += $"to={to.Value:yyyy-MM-dd}&";
        if (projectId.HasValue) url += $"projectId={projectId.Value}&";

        var response = await api.GetAsync(url.TrimEnd('&', '?'));
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<DeviationReportDto>();
        return null;
    }

    public async Task<CertificationReportDto?> GetCertificationReportAsync()
    {
        var response = await api.GetAsync("api/reports/certifications");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CertificationReportDto>();
        return null;
    }

    public async Task<SafetyReportDto?> GetSafetyReportAsync(
        DateOnly? from = null, DateOnly? to = null)
    {
        var url = "api/reports/safety?";
        if (from.HasValue) url += $"from={from.Value:yyyy-MM-dd}&";
        if (to.HasValue) url += $"to={to.Value:yyyy-MM-dd}&";

        var response = await api.GetAsync(url.TrimEnd('&', '?'));
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<SafetyReportDto>();
        return null;
    }

    public async Task<ProjectReportSummaryDto?> GetProjectSummaryAsync(Guid projectId)
    {
        var response = await api.GetAsync($"api/reports/project/{projectId}/summary");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProjectReportSummaryDto>();
        return null;
    }

    // ─── CSV Exports ───

    public async Task<byte[]?> ExportHoursCsvAsync(DateOnly? from, DateOnly? to, Guid? projectId = null, Guid? personId = null)
    {
        var url = "api/reports/hours/export?";
        if (from.HasValue) url += $"from={from.Value:yyyy-MM-dd}&";
        if (to.HasValue) url += $"to={to.Value:yyyy-MM-dd}&";
        if (projectId.HasValue) url += $"projectId={projectId.Value}&";
        if (personId.HasValue) url += $"personId={personId.Value}&";
        var r = await api.GetAsync(url.TrimEnd('&', '?'));
        return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> ExportDeviationsCsvAsync(DateOnly? from, DateOnly? to, string? severity = null, string? status = null, Guid? projectId = null)
    {
        var url = "api/reports/deviations/export?";
        if (from.HasValue) url += $"from={from.Value:yyyy-MM-dd}&";
        if (to.HasValue) url += $"to={to.Value:yyyy-MM-dd}&";
        if (!string.IsNullOrEmpty(severity)) url += $"severity={severity}&";
        if (!string.IsNullOrEmpty(status)) url += $"status={status}&";
        if (projectId.HasValue) url += $"projectId={projectId.Value}&";
        var r = await api.GetAsync(url.TrimEnd('&', '?'));
        return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> ExportCertificationsCsvAsync(string? type = null, Guid? personId = null, string? status = null)
    {
        var url = "api/reports/certifications/export?";
        if (!string.IsNullOrEmpty(type)) url += $"type={Uri.EscapeDataString(type)}&";
        if (personId.HasValue) url += $"personId={personId.Value}&";
        if (!string.IsNullOrEmpty(status)) url += $"status={status}&";
        var r = await api.GetAsync(url.TrimEnd('&', '?'));
        return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> ExportSafetyCsvAsync(DateOnly? from, DateOnly? to, string? type = null)
    {
        var url = "api/reports/safety/export?";
        if (from.HasValue) url += $"from={from.Value:yyyy-MM-dd}&";
        if (to.HasValue) url += $"to={to.Value:yyyy-MM-dd}&";
        if (!string.IsNullOrEmpty(type)) url += $"type={Uri.EscapeDataString(type)}&";
        var r = await api.GetAsync(url.TrimEnd('&', '?'));
        return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> ExportProjectPersonnelCsvAsync(Guid projectId)
    {
        var r = await api.GetAsync($"api/reports/project/{projectId}/personnel/export");
        return r.IsSuccessStatusCode ? await r.Content.ReadAsByteArrayAsync() : null;
    }
}
