using System.Net.Http.Json;
using Solodoc.Shared.Employees;

namespace Solodoc.Client.Services;

public class EmployeeService(ApiHttpClient api)
{
    public async Task<List<EmployeeListItemDto>> GetEmployeesAsync()
    {
        var response = await api.GetAsync("api/employees");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<EmployeeListItemDto>>() ?? [];
        return [];
    }

    public async Task<List<EmployeeListItemDto>> GetProjectCrewAsync(Guid projectId)
    {
        var response = await api.GetAsync($"api/projects/{projectId}/crew");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<EmployeeListItemDto>>() ?? [];
        return [];
    }

    public async Task<EmployeeDetailDto?> GetEmployeeAsync(Guid personId)
    {
        var response = await api.GetAsync($"api/employees/{personId}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<EmployeeDetailDto>();
        return null;
    }

    public async Task<bool> InviteAsync(InviteEmployeeRequest request)
    {
        var response = await api.PostAsJsonAsync("api/employees/invite", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SuspendAsync(Guid personId)
    {
        var response = await api.PatchAsync($"api/employees/{personId}/suspend");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ActivateAsync(Guid personId)
    {
        var response = await api.PatchAsync($"api/employees/{personId}/activate");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveAsync(Guid personId)
    {
        var response = await api.DeleteAsync($"api/employees/{personId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<CertificationDto>> GetCertificationsAsync(Guid personId)
    {
        var response = await api.GetAsync($"api/employees/{personId}/certifications");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<CertificationDto>>() ?? [];
        return [];
    }

    public async Task<bool> AddCertificationAsync(Guid personId, CreateCertificationRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/employees/{personId}/certifications", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<TrainingDto>> GetTrainingAsync(Guid personId)
    {
        var response = await api.GetAsync($"api/employees/{personId}/training");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<TrainingDto>>() ?? [];
        return [];
    }

    public async Task<ProfileDto?> GetProfileAsync()
    {
        var response = await api.GetAsync("api/profile");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ProfileDto>();
        return null;
    }

    public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var response = await api.PutAsJsonAsync("api/profile", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<VacationOverviewDto?> GetVacationAsync(Guid personId)
    {
        var response = await api.GetAsync($"api/employees/{personId}/vacation");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<VacationOverviewDto>();
        return null;
    }

    public async Task<bool> CreateVacationEntryAsync(Guid personId, CreateVacationEntryRequest request)
    {
        var response = await api.PostAsJsonAsync($"api/employees/{personId}/vacation", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ApproveVacationAsync(Guid personId, Guid entryId)
    {
        var response = await api.PatchAsync($"api/employees/{personId}/vacation/{entryId}/approve");
        return response.IsSuccessStatusCode;
    }
}
