using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Solodoc.Client.Services;

/// <summary>
/// Extends ApiHttpClient with offline-aware caching.
/// GET requests: cache-first when offline, network-first when online.
/// Write requests: queue when offline, send immediately when online.
/// </summary>
public class OfflineAwareApiClient(ApiHttpClient api, OfflineStorageService storage, IJSRuntime js)
{
    /// <summary>
    /// GET with offline cache support.
    /// Online: fetch from API, cache result. Offline: return cached data.
    /// </summary>
    public async Task<T?> GetWithCacheAsync<T>(string url, string entityType, int maxAgeMs = 3600000) where T : class
    {
        var isOnline = await storage.IsOnlineAsync();

        if (isOnline)
        {
            try
            {
                var response = await api.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<T>();
                    if (data is not null)
                    {
                        // Cache for offline use
                        await storage.CacheAsync(url, entityType, data);
                    }
                    return data;
                }
            }
            catch
            {
                // Network error — fall through to cache
            }
        }

        // Offline or network error: try cache
        return await storage.GetCachedAsync<T>(url, maxAgeMs);
    }

    /// <summary>
    /// POST with offline queue support.
    /// Online: send immediately. Offline: queue for later.
    /// </summary>
    public async Task<bool> PostWithQueueAsync(string url, string entityType, object payload)
    {
        var isOnline = await storage.IsOnlineAsync();

        if (isOnline)
        {
            try
            {
                var response = await api.PostAsJsonAsync(url, payload);
                return response.IsSuccessStatusCode;
            }
            catch { }
        }

        // Queue for later
        await storage.AddToSyncQueueAsync(entityType, "POST", url, payload);
        return true; // Return true to indicate "accepted" (will sync later)
    }

    /// <summary>
    /// PUT with offline queue support.
    /// </summary>
    public async Task<bool> PutWithQueueAsync(string url, string entityType, object payload)
    {
        var isOnline = await storage.IsOnlineAsync();

        if (isOnline)
        {
            try
            {
                var response = await api.PutAsJsonAsync(url, payload);
                return response.IsSuccessStatusCode;
            }
            catch { }
        }

        await storage.AddToSyncQueueAsync(entityType, "PUT", url, payload);
        return true;
    }

    /// <summary>
    /// PATCH with offline queue support.
    /// </summary>
    public async Task<bool> PatchWithQueueAsync(string url, string entityType, object? payload = null)
    {
        var isOnline = await storage.IsOnlineAsync();

        if (isOnline)
        {
            try
            {
                var response = payload is not null
                    ? await api.PatchAsJsonAsync(url, payload)
                    : await api.PatchAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch { }
        }

        await storage.AddToSyncQueueAsync(entityType, "PATCH", url, payload);
        return true;
    }
}
