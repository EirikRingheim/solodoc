using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Solodoc.Application.Services;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.Infrastructure.Services;

public class PowerOfficeService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    SolodocDbContext db,
    ILogger<PowerOfficeService> logger) : IPowerOfficeService
{
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    private bool UseDemoApi => configuration.GetValue<bool>("PowerOffice:UseDemoApi", true);
    private string AppKey => configuration["PowerOffice:ApplicationKey"] ?? "";
    private string ClientKey => configuration["PowerOffice:ClientKey"] ?? "";
    private string SubscriptionKey => configuration["PowerOffice:SubscriptionKey"] ?? "";

    private string AuthUrl => UseDemoApi
        ? "https://goapi.poweroffice.net/Demo/OAuth/Token"
        : "https://goapi.poweroffice.net/OAuth/Token";

    private string BaseUrl => UseDemoApi
        ? "https://goapi.poweroffice.net/demo/v2"
        : "https://goapi.poweroffice.net/v2";

    // ── Auth ──

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-2))
            return _cachedToken;

        var client = httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{AppKey}:{ClientKey}"));

        var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Headers.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
        request.Content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);

        var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("PowerOffice auth failed: {Status} {Body}", response.StatusCode, body);
            throw new Exception($"PowerOffice auth failed: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        _cachedToken = result?.AccessToken ?? throw new Exception("No access token in response");
        _tokenExpiry = DateTimeOffset.UtcNow.AddMinutes(18); // 20 min lifetime, refresh 2 min early
        logger.LogInformation("PowerOffice token obtained, expires at {Expiry}", _tokenExpiry);
        return _cachedToken;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync(CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct);
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
        return client;
    }

    // ── Public API ──

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var client = await GetAuthenticatedClientAsync(ct);
            var response = await client.GetAsync("Employees", ct);
            logger.LogInformation("PowerOffice connection test: {Status}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PowerOffice connection test failed");
            return false;
        }
    }

    public async Task<List<PowerOfficeEmployee>> GetEmployeesAsync(CancellationToken ct = default)
    {
        var client = await GetAuthenticatedClientAsync(ct);
        var response = await client.GetAsync("Employees", ct);
        response.EnsureSuccessStatusCode();

        var employees = await response.Content.ReadFromJsonAsync<List<PoEmployeeDto>>(ct);
        return employees?.Select(e => new PowerOfficeEmployee(
            e.Id, e.FirstName ?? "", e.LastName ?? "", e.EmailAddress
        )).ToList() ?? [];
    }

    public async Task SyncHoursAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var client = await GetAuthenticatedClientAsync(ct);

        // Get approved hours for the period
        var entries = await db.TimeEntries
            .Where(t => t.TenantId == tenantId && t.Date >= from && t.Date <= to
                && (t.Status == TimeEntryStatus.Approved || t.Status == TimeEntryStatus.Submitted))
            .ToListAsync(ct);

        logger.LogInformation("Syncing {Count} hour entries to PowerOffice for {From}-{To}", entries.Count, from, to);

        foreach (var entry in entries)
        {
            // Check if already synced (using ExternalImportReference)
            var extRef = $"solodoc-hours-{entry.Id}";

            var salaryLine = new
            {
                employeeId = (long?)null, // TODO: map Solodoc person to PowerOffice employee ID
                quantity = (double)entry.Hours,
                fromDate = entry.Date.ToDateTime(TimeOnly.MinValue),
                toDate = entry.Date.ToDateTime(TimeOnly.MaxValue),
                comment = $"Solodoc: {entry.Category}",
                externalImportReference = extRef
            };

            try
            {
                var response = await client.PostAsJsonAsync("SalaryLines", salaryLine, ct);
                if (response.IsSuccessStatusCode)
                    logger.LogInformation("Synced hours {EntryId} to PowerOffice", entry.Id);
                else
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    logger.LogWarning("Failed to sync hours {EntryId}: {Status} {Body}", entry.Id, response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error syncing hours {EntryId}", entry.Id);
            }
        }
    }

    public async Task SyncExpensesAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var client = await GetAuthenticatedClientAsync(ct);

        var expenses = await db.Expenses
            .Where(e => e.TenantId == tenantId && e.Date >= from && e.Date <= to
                && (e.Status == ExpenseStatus.Approved || e.Status == ExpenseStatus.Paid))
            .ToListAsync(ct);

        logger.LogInformation("Syncing {Count} expenses to PowerOffice for {From}-{To}", expenses.Count, from, to);

        foreach (var expense in expenses)
        {
            var extRef = $"solodoc-expense-{expense.Id}";

            var salaryLine = new
            {
                employeeId = (long?)null, // TODO: map
                amount = (double)expense.Amount,
                fromDate = expense.Date.ToDateTime(TimeOnly.MinValue),
                toDate = expense.Date.ToDateTime(TimeOnly.MaxValue),
                comment = $"Solodoc utlegg: {expense.Description ?? expense.Category.ToString()}",
                externalImportReference = extRef
            };

            try
            {
                var response = await client.PostAsJsonAsync("SalaryLines", salaryLine, ct);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    logger.LogWarning("Failed to sync expense {Id}: {Status} {Body}", expense.Id, response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error syncing expense {Id}", expense.Id);
            }
        }
    }

    // ── Internal DTOs ──

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);

    private record ODataResponse<T>(List<T>? Value);

    private record PoEmployeeDto(
        long Id,
        string? FirstName,
        string? LastName,
        string? EmailAddress);
}
