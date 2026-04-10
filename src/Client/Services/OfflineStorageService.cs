using System.Text.Json;
using Microsoft.JSInterop;

namespace Solodoc.Client.Services;

/// <summary>
/// Wraps IndexedDB operations via JS interop for offline data storage.
/// </summary>
public class OfflineStorageService(IJSRuntime js)
{
    // ── Sync Queue ──────────────────────────────────────

    public async Task<int> AddToSyncQueueAsync(string entityType, string action, string url, object? payload)
    {
        var payloadJson = payload is not null ? JsonSerializer.Serialize(payload) : null;
        return await js.InvokeAsync<int>("solodocDb.addToSyncQueue",
            entityType, action, url, payloadJson, null, null);
    }

    public async Task<List<SyncQueueEntry>> GetSyncQueueAsync()
    {
        return await js.InvokeAsync<List<SyncQueueEntry>>("solodocDb.getSyncQueue");
    }

    public async Task<int> GetSyncQueueCountAsync()
    {
        return await js.InvokeAsync<int>("solodocDb.getSyncQueueCount");
    }

    public async Task UpdateSyncQueueEntryAsync(int id, string status, int? retryCount = null)
    {
        var updates = new Dictionary<string, object> { ["status"] = status };
        if (retryCount.HasValue) updates["retryCount"] = retryCount.Value;
        await js.InvokeVoidAsync("solodocDb.updateSyncQueueEntry", id, updates);
    }

    public async Task RemoveSyncQueueEntryAsync(int id)
    {
        await js.InvokeVoidAsync("solodocDb.removeSyncQueueEntry", id);
    }

    public async Task ClearCompletedAsync()
    {
        await js.InvokeVoidAsync("solodocDb.clearCompletedSyncEntries");
    }

    // ── Entity Cache ────────────────────────────────────

    public async Task CacheAsync<T>(string key, string entityType, T data)
    {
        await js.InvokeVoidAsync("solodocDb.cacheEntity", key, entityType, data);
    }

    public async Task<T?> GetCachedAsync<T>(string key, int maxAgeMs = 3600000) where T : class
    {
        return await js.InvokeAsync<T?>("solodocDb.getCachedEntity", key, maxAgeMs);
    }

    public async Task ClearCacheAsync(string entityType)
    {
        await js.InvokeVoidAsync("solodocDb.clearCacheByType", entityType);
    }

    // ── Metadata ────────────────────────────────────────

    public async Task SetMetaAsync(string key, object value)
    {
        await js.InvokeVoidAsync("solodocDb.setMeta", key, value);
    }

    public async Task<T?> GetMetaAsync<T>(string key)
    {
        return await js.InvokeAsync<T?>("solodocDb.getMeta", key);
    }

    // ── Local State ───────────────────────────────────

    public async Task SetLocalStateAsync<T>(string key, T value)
    {
        await js.InvokeVoidAsync("solodocDb.setLocalState", key, value);
    }

    public async Task<T?> GetLocalStateAsync<T>(string key)
    {
        return await js.InvokeAsync<T?>("solodocDb.getLocalState", key);
    }

    public async Task RemoveLocalStateAsync(string key)
    {
        await js.InvokeVoidAsync("solodocDb.removeLocalState", key);
    }

    // ── Offline Photos ──────────────────────────────────

    public async Task<int> SaveOfflinePhotoAsync(string entityType, string entityId, string fileName, string base64Data, string contentType)
    {
        return await js.InvokeAsync<int>("solodocDb.saveOfflinePhoto", entityType, entityId, fileName, base64Data, contentType);
    }

    public async Task<List<OfflinePhotoEntry>> GetOfflinePhotosAsync()
    {
        return await js.InvokeAsync<List<OfflinePhotoEntry>>("solodocDb.getOfflinePhotos");
    }

    public async Task RemoveOfflinePhotoAsync(int id)
    {
        await js.InvokeVoidAsync("solodocDb.removeOfflinePhoto", id);
    }

    // ── Connectivity ────────────────────────────────────

    public async Task<bool> IsOnlineAsync()
    {
        return await js.InvokeAsync<bool>("solodocDb.isOnline");
    }
}

public class OfflinePhotoEntry
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Base64Data { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SavedAt { get; set; }
}

public class SyncQueueEntry
{
    public int Id { get; set; }
    public string EntityType { get; set; } = "";
    public string Action { get; set; } = "";
    public string Url { get; set; } = "";
    public string? Payload { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public long Timestamp { get; set; }
    public int RetryCount { get; set; }
    public string Status { get; set; } = "pending";
}
