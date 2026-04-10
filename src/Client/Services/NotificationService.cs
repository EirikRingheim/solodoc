using System.Net.Http.Json;
using Solodoc.Shared.Notifications;

namespace Solodoc.Client.Services;

public class NotificationService(ApiHttpClient api)
{
    public async Task<List<NotificationDto>> GetNotificationsAsync()
    {
        var response = await api.GetAsync("api/notifications");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<NotificationDto>>() ?? [];
        return [];
    }

    public async Task MarkReadAsync(Guid id)
    {
        await api.PatchAsync($"api/notifications/{id}/read");
    }

    public async Task MarkAllReadAsync()
    {
        await api.PatchAsync("api/notifications/read-all");
    }

    public async Task<List<AnnouncementDto>> GetAnnouncementsAsync()
    {
        var response = await api.GetAsync("api/announcements");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<AnnouncementDto>>() ?? [];
        return [];
    }

    public async Task AcknowledgeAnnouncementAsync(Guid id)
    {
        await api.PostAsJsonAsync($"api/announcements/{id}/acknowledge", new { });
    }
}
