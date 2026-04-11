using System.Net.Http.Json;
using Solodoc.Shared.Marketplace;

namespace Solodoc.Client.Services;

public class MarketplaceService(ApiHttpClient api)
{
    public async Task<List<MarketplaceListItemDto>> GetTemplatesAsync()
    {
        var r = await api.GetAsync("api/marketplace");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<MarketplaceListItemDto>>() ?? [] : [];
    }

    public async Task<bool> PublishAsync(Guid templateId, PublishToMarketplaceRequest request)
    {
        return (await api.PostAsJsonAsync($"api/marketplace/publish/{templateId}", request)).IsSuccessStatusCode;
    }

    public async Task<BuyResult?> BuyAsync(Guid marketplaceId)
    {
        var r = await api.PostAsJsonAsync($"api/marketplace/buy/{marketplaceId}", new { });
        if (r.IsSuccessStatusCode)
            return await r.Content.ReadFromJsonAsync<BuyResult>();
        return null;
    }

    public async Task<List<PurchaseDto>> GetPurchasesAsync()
    {
        var r = await api.GetAsync("api/marketplace/purchases");
        return r.IsSuccessStatusCode ? await r.Content.ReadFromJsonAsync<List<PurchaseDto>>() ?? [] : [];
    }

    public async Task<bool> UnpublishAsync(Guid id)
    {
        return (await api.DeleteAsync($"api/marketplace/{id}")).IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAsync(Guid id, PublishToMarketplaceRequest request)
    {
        return (await api.PutAsJsonAsync($"api/marketplace/{id}", request)).IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return (await api.DeleteAsync($"api/marketplace/{id}")).IsSuccessStatusCode;
    }

    public record BuyResult(Guid TemplateId, int Price);
}
