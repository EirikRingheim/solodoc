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

    public async Task<bool> CreateAnnouncementAsync(CreateAnnouncementRequest request)
    {
        var r = await api.PostAsJsonAsync("api/announcements", request);
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAnnouncementAsync(Guid id, CreateAnnouncementRequest request)
    {
        var r = await api.PutAsJsonAsync($"api/announcements/{id}", request);
        return r.IsSuccessStatusCode;
    }

    public async Task<bool> DismissAnnouncementAsync(Guid id)
        => (await api.PostAsJsonAsync($"api/announcements/{id}/dismiss", new { })).IsSuccessStatusCode;

    public async Task<bool> UndismissAnnouncementAsync(Guid id)
        => (await api.DeleteAsync($"api/announcements/{id}/dismiss")).IsSuccessStatusCode;

    public async Task<List<AnnouncementDto>> GetDismissedAnnouncementsAsync()
    {
        var response = await api.GetAsync("api/announcements/dismissed");
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<List<AnnouncementDto>>() ?? [];
        return [];
    }

    public async Task<bool> DeleteAnnouncementAsync(Guid id)
        => (await api.DeleteAsync($"api/announcements/{id}")).IsSuccessStatusCode;

    public async Task CommentOnAnnouncementAsync(Guid annId, string content)
    {
        await api.PostAsJsonAsync($"api/announcements/{annId}/comments", new CreateAnnouncementCommentRequest(content));
    }
}
