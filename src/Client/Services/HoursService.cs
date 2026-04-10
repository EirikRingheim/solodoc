using System.Net.Http.Json;
using Solodoc.Shared.Common;
using Solodoc.Shared.Hours;
using Solodoc.Shared.Projects;

namespace Solodoc.Client.Services;

public class HoursService(ApiHttpClient api, OfflineAwareApiClient offlineApi)
{
    public async Task<PagedResult<TimeEntryListItemDto>> GetTimeEntriesAsync(
        int page = 1, int pageSize = 50, string? weekOf = null,
        Guid? projectId = null, string? status = null)
    {
        var url = $"api/hours?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(weekOf))
            url += $"&weekOf={Uri.EscapeDataString(weekOf)}";
        if (projectId.HasValue)
            url += $"&projectId={projectId.Value}";
        if (!string.IsNullOrWhiteSpace(status))
            url += $"&status={Uri.EscapeDataString(status)}";

        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<PagedResult<TimeEntryListItemDto>>()
                   ?? new([], 0, page, pageSize);
        return new([], 0, page, pageSize);
    }

    public async Task<PagedResult<TimeEntryListItemDto>> GetAdminTimeEntriesAsync(
        int page = 1, int pageSize = 50, Guid? personId = null,
        Guid? projectId = null, string? weekOf = null, string? status = null)
    {
        var url = $"api/hours/admin?page={page}&pageSize={pageSize}";
        if (personId.HasValue) url += $"&personId={personId.Value}";
        if (projectId.HasValue) url += $"&projectId={projectId.Value}";
        if (!string.IsNullOrWhiteSpace(weekOf)) url += $"&weekOf={Uri.EscapeDataString(weekOf)}";
        if (!string.IsNullOrWhiteSpace(status)) url += $"&status={Uri.EscapeDataString(status)}";

        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<PagedResult<TimeEntryListItemDto>>()
                   ?? new([], 0, page, pageSize);
        return new([], 0, page, pageSize);
    }

    public async Task<ActiveClockDto?> GetActiveClockAsync()
    {
        var response = await api.GetAsync("api/hours/active-clock");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ActiveClockDto>();
        return null;
    }

    public async Task<Guid?> ClockInAsync(ClockInRequest request)
    {
        var response = await api.PostAsJsonAsync("api/hours/clock-in", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }

        // If offline, queue and return placeholder
        var queued = await offlineApi.PostWithQueueAsync("api/hours/clock-in", "clockIn", request);
        if (queued) return Guid.NewGuid();
        return null;
    }

    public async Task<TimeEntryDetailDto?> ClockOutAsync(ClockOutRequest request)
    {
        var response = await api.PostAsJsonAsync("api/hours/clock-out", request);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<TimeEntryDetailDto>();

        // If offline, queue
        await offlineApi.PostWithQueueAsync("api/hours/clock-out", "clockOut", request);
        return null;
    }

    public async Task<bool> DeleteEntryAsync(Guid id)
    {
        var response = await api.DeleteAsync($"api/hours/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateEntryAsync(Guid id, ManualTimeEntryRequest request)
    {
        var response = await api.PutAsJsonAsync($"api/hours/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<Guid?> CreateManualEntryAsync(ManualTimeEntryRequest request)
    {
        var response = await api.PostAsJsonAsync("api/hours", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }

        // If offline, queue
        var queued = await offlineApi.PostWithQueueAsync("api/hours", "timeEntry", request);
        if (queued) return Guid.NewGuid();
        return null;
    }

    public async Task<bool> SubmitAsync(Guid id)
    {
        var response = await api.PatchAsync($"api/hours/{id}/submit");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ApproveAsync(Guid id)
    {
        var response = await api.PatchAsync($"api/hours/{id}/approve");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectAsync(Guid id, string? reason)
    {
        var response = await api.PatchAsJsonAsync($"api/hours/{id}/reject", new ApproveRejectRequest(reason));
        return response.IsSuccessStatusCode;
    }

    public async Task<MyScheduleDto?> GetMyScheduleAsync()
    {
        var response = await api.GetAsync("api/hours/my-schedule");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<MyScheduleDto>();
        return null;
    }

    public async Task<Guid?> AddAllowanceAsync(Guid timeEntryId, AddTimeEntryAllowanceRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/hours/{timeEntryId}/allowances", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<MyHeatmapDto?> GetMyHeatmapAsync(DateOnly from, DateOnly to)
    {
        var response = await api.GetAsync($"api/hours/my-heatmap?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<MyHeatmapDto>();
        return null;
    }

    public async Task<DayDetailDto?> GetDayDetailAsync(Guid personId, DateOnly date)
    {
        var response = await api.GetAsync($"api/hours/admin/day-detail?personId={personId}&date={date:yyyy-MM-dd}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<DayDetailDto>();
        return null;
    }

    public async Task<int> ApproveDayAsync(Guid personId, DateOnly date)
    {
        var response = await api.PostAsJsonAsync("api/hours/admin/approve-day", new ApproveDayRequest(personId, date));
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApproveCountResult>();
            return result?.Approved ?? 0;
        }
        return 0;
    }

    private record ApproveCountResult(int Approved);

    public async Task<HoursHeatmapDto?> GetHeatmapAsync(DateOnly from, DateOnly to)
    {
        var response = await api.GetAsync($"api/hours/heatmap?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<HoursHeatmapDto>();
        return null;
    }

    public async Task<List<WorkScheduleDto>> GetSchedulesAsync()
    {
        var response = await api.GetAsync("api/schedules");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<WorkScheduleDto>>() ?? [];
        return [];
    }

    public async Task<bool> CreateAllowanceRuleAsync(AllowanceRuleDto dto)
    {
        var response = await api.PostAsJsonAsync("api/allowances/rules", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<AllowanceRuleDto>> GetAllowanceRulesAsync()
    {
        var response = await api.GetAsync("api/allowances/rules");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<AllowanceRuleDto>>() ?? [];
        return [];
    }

    // For hours dialog — show all projects, not just active
    public async Task<List<ProjectListItemDto>> GetAllProjectsAsync()
    {
        var url = "api/projects?page=1&pageSize=200&sortDesc=false&sortBy=name";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<PagedResult<ProjectListItemDto>>();
            return result?.Items ?? [];
        }
        return [];
    }

    // ─── Absences ─────────────────────────────────────────

    public async Task<List<AbsenceListItemDto>> GetMyAbsencesForRangeAsync(DateOnly from, DateOnly to)
    {
        var url = $"api/absences?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<AbsenceListItemDto>>() ?? [];
        return [];
    }

    public async Task<List<AbsenceListItemDto>> GetMyAbsencesAsync(int? year = null)
    {
        var url = "api/absences";
        if (year.HasValue) url += $"?year={year.Value}";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<AbsenceListItemDto>>() ?? [];
        return [];
    }

    public async Task<Guid?> CreateAbsenceAsync(CreateAbsenceRequest request)
    {
        var response = await api.PostAsJsonAsync("api/absences", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> ApproveAbsenceAsync(Guid id)
    {
        var response = await api.PatchAsync($"api/absences/{id}/approve");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectAbsenceAsync(Guid id)
    {
        var response = await api.PatchAsync($"api/absences/{id}/reject");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CreditOvertimeBankAsync(Guid timeEntryId, decimal hours)
    {
        var response = await api.PostAsJsonAsync("api/hours/overtime-bank/credit",
            new { timeEntryId, hours });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAbsenceAsync(Guid id)
    {
        var response = await api.DeleteAsync($"api/absences/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<BalanceSummaryDto?> GetMyBalanceAsync()
    {
        var response = await api.GetAsync("api/hours/balance");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<BalanceSummaryDto>();
        return null;
    }

    public async Task<BalanceSummaryDto?> GetEmployeeBalanceAsync(Guid personId)
    {
        var response = await api.GetAsync($"api/hours/balance/{personId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<BalanceSummaryDto>();
        return null;
    }

    private record IdResponse(Guid Id);
}
