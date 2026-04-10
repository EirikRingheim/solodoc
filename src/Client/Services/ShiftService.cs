using System.Net.Http.Json;
using Solodoc.Shared.Hours;

namespace Solodoc.Client.Services;

public class ShiftService(ApiHttpClient api)
{
    // ─── Shift Definitions ───────────────────────────
    public async Task<List<ShiftDefinitionDto>> GetShiftsAsync()
    {
        var r = await api.GetAsync("api/shifts");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<ShiftDefinitionDto>>() ?? [] : [];
    }

    public async Task<Guid?> CreateShiftAsync(CreateShiftDefinitionRequest req)
    {
        var r = await api.PostAsJsonAsync("api/shifts", req);
        if (r.IsSuccessStatusCode) { var res = await r.Content.ReadFromJsonAsync<IdRes>(); return res?.Id; }
        return null;
    }

    public async Task<bool> UpdateShiftAsync(Guid id, CreateShiftDefinitionRequest req)
    {
        var r = await api.PutAsJsonAsync($"api/shifts/{id}", req);
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteShiftAsync(Guid id)
    {
        return (await api.DeleteAsync($"api/shifts/{id}")).IsSuccessStatusCode;
    }

    // ─── Rotation Patterns ───────────────────────────
    public async Task<List<RotationPatternDto>> GetRotationsAsync()
    {
        var r = await api.GetAsync("api/rotations");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<RotationPatternDto>>() ?? [] : [];
    }

    public async Task<Guid?> CreateRotationAsync(CreateRotationPatternRequest req)
    {
        var r = await api.PostAsJsonAsync("api/rotations", req);
        if (r.IsSuccessStatusCode) { var res = await r.Content.ReadFromJsonAsync<IdRes>(); return res?.Id; }
        return null;
    }

    public async Task<bool> DeleteRotationAsync(Guid id)
    {
        return (await api.DeleteAsync($"api/rotations/{id}")).IsSuccessStatusCode;
    }

    // ─── Assignments ─────────────────────────────────
    public async Task<List<EmployeeRotationAssignmentDto>> GetAssignmentsAsync()
    {
        var r = await api.GetAsync("api/rotations/assignments");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<EmployeeRotationAssignmentDto>>() ?? [] : [];
    }

    public async Task<bool> AssignRotationAsync(AssignRotationRequest req)
    {
        return (await api.PostAsJsonAsync("api/rotations/assignments", req)).IsSuccessStatusCode;
    }

    public async Task<bool> RemoveAssignmentAsync(Guid id)
    {
        return (await api.DeleteAsync($"api/rotations/assignments/{id}")).IsSuccessStatusCode;
    }

    // ─── Today's Shift ───────────────────────────────
    public async Task<TodayShiftDto?> GetTodayShiftAsync()
    {
        var r = await api.GetAsync("api/shifts/today");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<TodayShiftDto>() : null;
    }

    // ─── Overtime Rules ──────────────────────────────
    public async Task<List<OvertimeRuleDto>> GetOvertimeRulesAsync()
    {
        var r = await api.GetAsync("api/overtime-rules");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<OvertimeRuleDto>>() ?? [] : [];
    }

    public async Task<Guid?> CreateOvertimeRuleAsync(CreateOvertimeRuleRequest req)
    {
        var r = await api.PostAsJsonAsync("api/overtime-rules", req);
        if (r.IsSuccessStatusCode) { var res = await r.Content.ReadFromJsonAsync<IdRes>(); return res?.Id; }
        return null;
    }

    public async Task<bool> UpdateOvertimeRuleAsync(Guid id, CreateOvertimeRuleRequest req)
    {
        return (await api.PutAsJsonAsync($"api/overtime-rules/{id}", req)).IsSuccessStatusCode;
    }

    public async Task<bool> DeleteOvertimeRuleAsync(Guid id)
    {
        return (await api.DeleteAsync($"api/overtime-rules/{id}")).IsSuccessStatusCode;
    }

    // ─── Planner ─────────────────────────────────────
    public async Task<List<PlannerRowDto>> GetPlannerDataAsync(DateOnly from, DateOnly to)
    {
        var r = await api.GetAsync($"api/planner?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<PlannerRowDto>>() ?? [] : [];
    }

    public async Task<bool> PlannerClearAsync(Guid personId, DateOnly from, DateOnly to)
    {
        var req = new PlannerAssignRequest(personId, from, to, "clear", null, null, null, null);
        return (await api.PostAsJsonAsync("api/planner/clear", req)).IsSuccessStatusCode;
    }

    public async Task<bool> PlannerAssignAsync(PlannerAssignRequest req)
    {
        return (await api.PostAsJsonAsync("api/planner/assign", req)).IsSuccessStatusCode;
    }

    // ─── My Plan ─────────────────────────────────────────
    public async Task<List<EmployeeCalendarDayDto>> GetMyPlanAsync(DateOnly from, DateOnly to)
    {
        var r = await api.GetAsync($"api/shifts/my-plan?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<EmployeeCalendarDayDto>>() ?? [] : [];
    }

    // ─── Employee Calendar ─────────────────────────────
    public async Task<EmployeeCalendarDto?> GetEmployeeCalendarAsync(Guid personId, DateOnly from, DateOnly to)
    {
        var r = await api.GetAsync($"api/planner/calendar/{personId}?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<EmployeeCalendarDto>() : null;
    }

    // ─── Hours Settings ──────────────────────────────
    public async Task<HoursSettingsDto?> GetHoursSettingsAsync()
    {
        var r = await api.GetAsync("api/hours-settings");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<HoursSettingsDto>() : null;
    }

    public async Task<bool> UpdateHoursSettingsAsync(UpdateHoursSettingsRequest req)
    {
        return (await api.PutAsJsonAsync("api/hours-settings", req)).IsSuccessStatusCode;
    }

    private record IdRes(Guid Id);
}
