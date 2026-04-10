using System.Net.Http.Json;
using Solodoc.Shared.Contacts;

namespace Solodoc.Client.Services;

public class ContactService(ApiHttpClient api)
{
    public async Task<List<ContactListItemDto>> GetContactsAsync(string? search = null)
    {
        var url = "api/contacts";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";
        var response = await api.GetAsync(url);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<ContactListItemDto>>() ?? [];
        return [];
    }

    public async Task<ContactDetailDto?> GetContactByIdAsync(Guid id)
    {
        var response = await api.GetAsync($"api/contacts/{id}");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<ContactDetailDto>();
        return null;
    }

    public async Task<Guid?> CreateContactAsync(CreateContactRequest request)
    {
        var response = await api.PostAsJsonAsync("api/contacts", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<IdResponse>();
            return result?.Id;
        }
        return null;
    }

    public async Task<bool> UpdateContactAsync(Guid id, CreateContactRequest request)
    {
        var response = await api.PutAsJsonAsync($"api/contacts/{id}", request);
        return response.IsSuccessStatusCode;
    }

    private record IdResponse(Guid Id);
}
