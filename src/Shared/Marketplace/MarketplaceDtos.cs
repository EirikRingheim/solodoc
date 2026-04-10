namespace Solodoc.Shared.Marketplace;

public record MarketplaceListItemDto(
    Guid Id, string Name, string? Description, string Category, string? Color,
    int ItemCount, int PriceKr, int PurchaseCount, bool AlreadyPurchased = false);

public record PublishToMarketplaceRequest(
    string? Name, string? Description, string? Category, string? Color, int? PriceKr);

public record PurchaseDto(
    Guid Id, string TemplateName, Guid CopiedTemplateId, int PriceKr, DateTimeOffset PurchasedAt);
