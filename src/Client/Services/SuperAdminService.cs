using System.Net.Http.Json;
using Solodoc.Shared.Admin;

namespace Solodoc.Client.Services;

public class SuperAdminService(ApiHttpClient api)
{
    // Tenants
    public async Task<List<TenantOverviewDto>> GetTenantsAsync()
    {
        var r = await api.GetAsync("api/admin/tenants");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<TenantOverviewDto>>() ?? [] : [];
    }

    public async Task<TenantDetailDto?> GetTenantDetailAsync(Guid id)
    {
        var r = await api.GetAsync($"api/admin/tenants/{id}");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<TenantDetailDto>() : null;
    }

    public async Task<bool> FreezeTenantAsync(Guid id)
        => (await api.PostAsJsonAsync($"api/admin/tenants/{id}/freeze", new { })).IsSuccessStatusCode;

    public async Task<bool> UnfreezeTenantAsync(Guid id)
        => (await api.PostAsJsonAsync($"api/admin/tenants/{id}/unfreeze", new { })).IsSuccessStatusCode;

    // Coupons
    public async Task<List<CouponCodeDto>> GetCouponsAsync()
    {
        var r = await api.GetAsync("api/admin/coupons");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<CouponCodeDto>>() ?? [] : [];
    }

    public async Task<CouponCodeDto?> CreateCouponAsync(CreateCouponRequest req)
    {
        var r = await api.PostAsJsonAsync("api/admin/coupons", req);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<CouponCodeDto>() : null;
    }

    public async Task<bool> DeleteCouponAsync(Guid id)
        => (await api.DeleteAsync($"api/admin/coupons/{id}")).IsSuccessStatusCode;

    // Invoices
    public async Task<List<TenantInvoiceDto>> GetInvoicesAsync(int? year = null, string? status = null)
    {
        var url = "api/admin/invoices";
        var q = new List<string>();
        if (year.HasValue) q.Add($"year={year}");
        if (!string.IsNullOrEmpty(status)) q.Add($"status={status}");
        if (q.Count > 0) url += "?" + string.Join("&", q);

        var r = await api.GetAsync(url);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<TenantInvoiceDto>>() ?? [] : [];
    }

    public async Task<int> GenerateInvoicesAsync(int year, int month)
    {
        var r = await api.PostAsJsonAsync("api/admin/invoices/generate", new GenerateInvoicesRequest(year, month));
        if (r.IsSuccessStatusCode)
        {
            var result = await r.Content.ReadFromJsonAsync<GenerateResult>();
            return result?.Generated ?? 0;
        }
        return 0;
    }

    public async Task<bool> UpdateInvoiceStatusAsync(Guid id, string status)
        => (await api.PutAsJsonAsync($"api/admin/invoices/{id}/status", new UpdateInvoiceStatusRequest(status))).IsSuccessStatusCode;

    public async Task<string?> GetEhfXmlAsync(Guid id)
    {
        var r = await api.GetAsync($"api/admin/invoices/{id}/ehf");
        return r.IsSuccessStatusCode ? await r.Content.ReadAsStringAsync() : null;
    }

    // Coupon redemption (for onboarding)
    public async Task<RedeemCouponResult?> RedeemCouponAsync(string code)
    {
        var r = await api.PostAsJsonAsync("api/coupons/redeem", new RedeemCouponRequest(code));
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<RedeemCouponResult>() : null;
    }

    // Client errors
    public async Task<List<ClientErrorDto>> GetErrorsAsync(bool includeResolved = false)
    {
        var url = includeResolved ? "api/admin/errors?includeResolved=true" : "api/admin/errors";
        var r = await api.GetAsync(url);
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<ClientErrorDto>>() ?? [] : [];
    }

    public async Task<bool> ResolveErrorAsync(Guid id)
        => (await api.PostAsJsonAsync($"api/admin/errors/{id}/resolve", new { })).IsSuccessStatusCode;

    public async Task ReportErrorAsync(string message, string? stackTrace, string? page)
    {
        try
        {
            await api.PostAsJsonAsync("api/errors/report", new ReportErrorRequest(
                message, stackTrace, page, null, null));
        }
        catch { /* silently fail — don't error while reporting errors */ }
    }

    private record GenerateResult(int Generated);
}
