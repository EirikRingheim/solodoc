using System.Net.Http.Json;
using Solodoc.Shared.Common;
using Solodoc.Shared.Expenses;

namespace Solodoc.Client.Services;

public class ExpenseService(ApiHttpClient api)
{
    // ── Receipts ──

    public async Task<(List<ExpenseListItemDto> Items, int Total)> GetExpensesAsync(
        int page = 1, int pageSize = 50, string? status = null, Guid? projectId = null,
        string? from = null, string? to = null)
    {
        var url = $"api/expenses?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={status}";
        if (projectId.HasValue) url += $"&projectId={projectId}";
        if (!string.IsNullOrEmpty(from)) url += $"&from={from}";
        if (!string.IsNullOrEmpty(to)) url += $"&to={to}";

        var r = await api.GetAsync(url);
        if (r.IsSuccessStatusCode)
        {
            var data = await r.Content.ReadFromJsonAsync<ExpenseListResult>();
            return (data?.Items ?? [], data?.TotalCount ?? 0);
        }
        return ([], 0);
    }

    public async Task<ExpenseDetailDto?> GetExpenseAsync(Guid id)
    {
        var r = await api.GetAsync($"api/expenses/{id}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ExpenseDetailDto>() : null;
    }

    public async Task<Guid?> CreateExpenseAsync(CreateExpenseRequest request)
    {
        var r = await api.PostAsJsonAsync("api/expenses", request);
        if (r.IsSuccessStatusCode)
        {
            var result = await r.Content.ReadFromJsonAsync<IdResult>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> SubmitExpenseAsync(Guid id) => (await api.PatchAsync($"api/expenses/{id}/submit")).IsSuccessStatusCode;
    public async Task<bool> ApproveExpenseAsync(Guid id) => (await api.PatchAsync($"api/expenses/{id}/approve")).IsSuccessStatusCode;
    public async Task<bool> RejectExpenseAsync(Guid id, string reason) => (await api.PatchAsJsonAsync($"api/expenses/{id}/reject", new { reason })).IsSuccessStatusCode;
    public async Task<bool> MarkExpensePaidAsync(Guid id) => (await api.PatchAsync($"api/expenses/{id}/mark-paid")).IsSuccessStatusCode;
    public async Task<bool> UndoPaidAsync(Guid id) => (await api.PatchAsync($"api/expenses/{id}/undo-paid")).IsSuccessStatusCode;
    public async Task<bool> UndoApproveAsync(Guid id) => (await api.PatchAsync($"api/expenses/{id}/undo-approve")).IsSuccessStatusCode;
    public async Task<bool> DeleteExpenseAsync(Guid id) => (await api.DeleteAsync($"api/expenses/{id}")).IsSuccessStatusCode;

    // ── Travel expenses ──

    public async Task<(List<TravelExpenseListItemDto> Items, int Total)> GetTravelExpensesAsync(
        int page = 1, int pageSize = 50, string? status = null)
    {
        var url = $"api/travel-expenses?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={status}";

        var r = await api.GetAsync(url);
        if (r.IsSuccessStatusCode)
        {
            var data = await r.Content.ReadFromJsonAsync<TravelExpenseListResult>();
            return (data?.Items ?? [], data?.TotalCount ?? 0);
        }
        return ([], 0);
    }

    public async Task<TravelExpenseDetailDto?> GetTravelExpenseAsync(Guid id)
    {
        var r = await api.GetAsync($"api/travel-expenses/{id}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<TravelExpenseDetailDto>() : null;
    }

    public async Task<Guid?> CreateTravelExpenseAsync(CreateTravelExpenseRequest request)
    {
        var r = await api.PostAsJsonAsync("api/travel-expenses", request);
        if (r.IsSuccessStatusCode)
        {
            var result = await r.Content.ReadFromJsonAsync<IdResult>();
            return result?.Id;
        }
        return null;
    }

    public async Task<TravelExpenseCalculationDto?> CalculateAsync(CreateTravelExpenseRequest request)
    {
        var r = await api.PostAsJsonAsync("api/travel-expenses/calculate", request);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<TravelExpenseCalculationDto>() : null;
    }

    public async Task<bool> SubmitTravelExpenseAsync(Guid id) => (await api.PatchAsync($"api/travel-expenses/{id}/submit")).IsSuccessStatusCode;
    public async Task<bool> ApproveTravelExpenseAsync(Guid id) => (await api.PatchAsync($"api/travel-expenses/{id}/approve")).IsSuccessStatusCode;
    public async Task<bool> MarkTravelExpensePaidAsync(Guid id) => (await api.PatchAsync($"api/travel-expenses/{id}/mark-paid")).IsSuccessStatusCode;

    // ── Settings ──

    public async Task<ExpenseSettingsDto?> GetSettingsAsync()
    {
        var r = await api.GetAsync("api/expense-settings");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<ExpenseSettingsDto>() : null;
    }

    public async Task<bool> UpdateSettingsAsync(UpdateExpenseSettingsRequest request)
        => (await api.PutAsJsonAsync("api/expense-settings", request)).IsSuccessStatusCode;

    // ── Rates ──

    public async Task<List<TravelExpenseRateDto>> GetRatesAsync()
    {
        var r = await api.GetAsync("api/travel-expense-rates");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<TravelExpenseRateDto>>() ?? [] : [];
    }

    public async Task<bool> CreateRateAsync(CreateTravelExpenseRateRequest request)
        => (await api.PostAsJsonAsync("api/travel-expense-rates", request)).IsSuccessStatusCode;

    // Internal result types
    private record IdResult(Guid Id);
    private record ExpenseListResult(List<ExpenseListItemDto> Items, int TotalCount);
    private record TravelExpenseListResult(List<TravelExpenseListItemDto> Items, int TotalCount);
}
