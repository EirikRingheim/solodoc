using System.Net.Http.Json;
using Solodoc.Shared.Auth;

namespace Solodoc.Client.Services;

public class RoleService(ApiHttpClient api)
{
    public async Task<List<CustomRoleDto>> GetRolesAsync()
    {
        var r = await api.GetAsync("api/roles");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<CustomRoleDto>>() ?? [] : [];
    }

    public async Task<Guid?> CreateRoleAsync(CreateCustomRoleRequest request)
    {
        var r = await api.PostAsJsonAsync("api/roles", request);
        if (r.IsSuccessStatusCode)
        {
            var result = await r.Content.ReadFromJsonAsync<IdResult>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> UpdateRoleAsync(Guid id, UpdateCustomRoleRequest request)
    {
        return (await api.PutAsJsonAsync($"api/roles/{id}", request)).IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRoleAsync(Guid id)
    {
        return (await api.DeleteAsync($"api/roles/{id}")).IsSuccessStatusCode;
    }

    public async Task<List<PermissionGroup>> GetPermissionDefinitionsAsync()
    {
        var r = await api.GetAsync("api/roles/permissions");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<PermissionGroup>>() ?? [] : [];
    }

    private record IdResult(Guid Id);
}
