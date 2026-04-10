using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Solodoc.Client.Services;

/// <summary>
/// Manages the offline sync queue: queues write operations when offline,
/// replays them when connectivity is restored.
/// </summary>
public class SyncService : IAsyncDisposable
{
    private readonly ApiHttpClient _api;
    private readonly OfflineStorageService _storage;
    private readonly IJSRuntime _js;
    private DotNetObjectReference<SyncService>? _dotNetRef;
    private bool _isSyncing;
    private bool _isOnline = true;

    public event Action<bool>? OnConnectivityChanged;
    public event Action<int>? OnQueueCountChanged;

    public bool IsOnline => _isOnline;
    public int PendingCount { get; private set; }
    public DateTimeOffset? LastSyncedAt { get; private set; }

    public SyncService(ApiHttpClient api, OfflineStorageService storage, IJSRuntime js)
    {
        _api = api;
        _storage = storage;
        _js = js;
    }

    /// <summary>
    /// Initialize connectivity monitoring and load initial state.
    /// </summary>
    public async Task InitializeAsync()
    {
        _isOnline = await _storage.IsOnlineAsync();
        PendingCount = await _storage.GetSyncQueueCountAsync();

        var lastSync = await _storage.GetMetaAsync<long?>("lastSyncedAt");
        if (lastSync.HasValue)
            LastSyncedAt = DateTimeOffset.FromUnixTimeMilliseconds(lastSync.Value);

        // Register for online/offline events
        _dotNetRef = DotNetObjectReference.Create(this);
        await _js.InvokeVoidAsync("solodocDb.registerConnectivityHandler", _dotNetRef, "OnConnectivityChangedCallback");
    }

    [JSInvokable]
    public async Task OnConnectivityChangedCallback(bool isOnline)
    {
        _isOnline = isOnline;
        OnConnectivityChanged?.Invoke(isOnline);

        if (isOnline)
        {
            // Automatically sync when back online
            await ProcessQueueAsync();
        }
    }

    /// <summary>
    /// Queue a write operation. If online, sends immediately. If offline, stores in IndexedDB.
    /// </summary>
    public async Task<bool> QueueOrSendAsync(string entityType, string action, string url, object? payload = null)
    {
        if (_isOnline)
        {
            // Try to send immediately
            var success = await SendRequestAsync(action, url, payload);
            if (success)
            {
                LastSyncedAt = DateTimeOffset.UtcNow;
                await _storage.SetMetaAsync("lastSyncedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                return true;
            }
            // If failed (server error, not connectivity), still queue it
        }

        // Queue for later
        await _storage.AddToSyncQueueAsync(entityType, action, url, payload);
        PendingCount = await _storage.GetSyncQueueCountAsync();
        OnQueueCountChanged?.Invoke(PendingCount);
        return false;
    }

    /// <summary>
    /// Process all pending items in the sync queue.
    /// </summary>
    public async Task ProcessQueueAsync()
    {
        if (_isSyncing || !_isOnline) return;
        _isSyncing = true;

        try
        {
            var queue = await _storage.GetSyncQueueAsync();
            if (queue.Count == 0) return;

            foreach (var entry in queue.OrderBy(e => e.Timestamp))
            {
                if (!_isOnline) break; // Stop if we went offline

                await _storage.UpdateSyncQueueEntryAsync(entry.Id, "sending");

                var success = await SendRequestAsync(entry.Action, entry.Url, entry.Payload);

                if (success)
                {
                    await _storage.RemoveSyncQueueEntryAsync(entry.Id);
                }
                else
                {
                    var newRetry = entry.RetryCount + 1;
                    if (newRetry >= 10)
                    {
                        // Give up after 10 retries
                        await _storage.UpdateSyncQueueEntryAsync(entry.Id, "failed", newRetry);
                    }
                    else
                    {
                        await _storage.UpdateSyncQueueEntryAsync(entry.Id, "pending", newRetry);
                        // Exponential backoff: wait before next item
                        var delay = Math.Min(1000 * Math.Pow(2, newRetry), 30000);
                        await Task.Delay((int)delay);
                    }
                }
            }

            PendingCount = await _storage.GetSyncQueueCountAsync();
            OnQueueCountChanged?.Invoke(PendingCount);

            if (PendingCount == 0)
            {
                LastSyncedAt = DateTimeOffset.UtcNow;
                await _storage.SetMetaAsync("lastSyncedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                await _storage.ClearCompletedAsync();
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private async Task<bool> SendRequestAsync(string action, string url, object? payload)
    {
        try
        {
            HttpResponseMessage response;

            // Parse payload if it's a string (from IndexedDB it comes as JSON string)
            HttpContent? content = null;
            if (payload is string jsonStr && !string.IsNullOrEmpty(jsonStr))
            {
                content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
            }
            else if (payload is not null and not string)
            {
                content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            }

            response = action.ToUpperInvariant() switch
            {
                "POST" => await _api.PostAsync(url, content ?? new StringContent("{}", Encoding.UTF8, "application/json")),
                "PUT" => content is not null
                    ? await _api.PutAsJsonAsync(url, JsonSerializer.Deserialize<object>(await content.ReadAsStringAsync()))
                    : await _api.PatchAsync(url),
                "PATCH" => await _api.PatchAsync(url),
                "DELETE" => await _api.DeleteAsync(url),
                _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            };

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("solodocDb.unregisterConnectivityHandler");
        }
        catch { }
        _dotNetRef?.Dispose();
    }
}
