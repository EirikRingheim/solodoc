using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;

namespace Solodoc.Client.Services;

public class ApiHttpClient
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly NavigationManager _navigation;
    private bool _isRefreshing;

    public ApiHttpClient(HttpClient http, ILocalStorageService localStorage, NavigationManager navigation)
    {
        _http = http;
        _localStorage = localStorage;
        _navigation = navigation;
    }

    public HttpClient Http => _http;

    public async Task AttachTokenAsync()
    {
        var token = await _localStorage.GetItemAsStringAsync("accessToken");
        if (!string.IsNullOrWhiteSpace(token))
        {
            token = token.Trim('"');
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }

        // Attach selected tenant
        _http.DefaultRequestHeaders.Remove("X-Tenant-Id");
        var tenantId = await _localStorage.GetItemAsStringAsync("selectedTenantId");
        if (!string.IsNullOrWhiteSpace(tenantId))
            _http.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.Trim('"'));
    }

    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        await AttachTokenAsync();
        var response = await _http.GetAsync(url);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            response = await TryRefreshAndRetryAsync(() => _http.GetAsync(url));
        return response;
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T value)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsJsonAsync(url, value);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            response = await TryRefreshAndRetryAsync(() => _http.PostAsJsonAsync(url, value));
        return response;
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string url, T value)
    {
        await AttachTokenAsync();
        var response = await _http.PutAsJsonAsync(url, value);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            response = await TryRefreshAndRetryAsync(() => _http.PutAsJsonAsync(url, value));
        return response;
    }

    public async Task<HttpResponseMessage> PostAsync(string url)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsync(url, null);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            response = await TryRefreshAndRetryAsync(() => _http.PostAsync(url, null));
        return response;
    }

    public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content)
    {
        await AttachTokenAsync();
        var response = await _http.PostAsync(url, content);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            response = await TryRefreshAndRetryAsync(() => _http.PostAsync(url, content));
        return response;
    }

    public async Task<HttpResponseMessage> PatchAsJsonAsync<T>(string url, T value)
    {
        await AttachTokenAsync();
        var response = await _http.PatchAsJsonAsync(url, value);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            response = await TryRefreshAndRetryAsync(() => _http.PatchAsJsonAsync(url, value));
        return response;
    }

    public async Task<HttpResponseMessage> PatchAsync(string url)
    {
        await AttachTokenAsync();
        var response = await _http.PatchAsync(url, null);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            response = await TryRefreshAndRetryAsync(() => _http.PatchAsync(url, null));
        return response;
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        await AttachTokenAsync();
        var response = await _http.DeleteAsync(url);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            response = await TryRefreshAndRetryAsync(() => _http.DeleteAsync(url));
        return response;
    }

    private async Task<HttpResponseMessage> TryRefreshAndRetryAsync(Func<Task<HttpResponseMessage>> retryRequest)
    {
        // If already refreshing, wait briefly then retry with whatever token is current
        if (_isRefreshing)
        {
            await Task.Delay(500);
            await AttachTokenAsync();
            return await retryRequest();
        }

        _isRefreshing = true;
        try
        {
            var refreshToken = await _localStorage.GetItemAsStringAsync("refreshToken");
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await ForceLogoutAsync();
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            refreshToken = refreshToken.Trim('"');

            // Try to refresh the token
            _http.DefaultRequestHeaders.Authorization = null;
            var refreshResponse = await _http.PostAsJsonAsync("api/auth/refresh",
                new { RefreshToken = refreshToken });

            if (refreshResponse.IsSuccessStatusCode)
            {
                var result = await refreshResponse.Content.ReadFromJsonAsync<RefreshResult>();
                if (result is not null)
                {
                    await _localStorage.SetItemAsStringAsync("accessToken", result.AccessToken);
                    await _localStorage.SetItemAsStringAsync("refreshToken", result.RefreshToken);

                    // Retry the original request with new token
                    await AttachTokenAsync();
                    return await retryRequest();
                }
            }

            // Refresh failed — log out
            await ForceLogoutAsync();
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private async Task ForceLogoutAsync()
    {
        await _localStorage.RemoveItemAsync("accessToken");
        await _localStorage.RemoveItemAsync("refreshToken");
        await _localStorage.RemoveItemAsync("selectedTenantId");
        _navigation.NavigateTo("/login", forceLoad: false);
    }

    private record RefreshResult(string AccessToken, string RefreshToken);
}
