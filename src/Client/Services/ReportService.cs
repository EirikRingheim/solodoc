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
}
