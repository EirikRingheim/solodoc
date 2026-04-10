using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Checklists;

/// <summary>
/// A template published to the marketplace by Solodoc super admin.
/// References a ChecklistTemplate as the source.
/// </summary>
public class MarketplaceTemplate : BaseEntity
{
    public Guid SourceTemplateId { get; set; }
    public Guid SourceTenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Generell"; // Bygg, Elektro, HMS, Generell, etc.
    public string? Color { get; set; }
    public int ItemCount { get; set; }
    public int PriceKr { get; set; } = 49;
    public bool IsPublished { get; set; }
    public int PurchaseCount { get; set; }

    // Navigation
    public ChecklistTemplate SourceTemplate { get; set; } = null!;
}

/// <summary>
/// Record of a tenant purchasing a marketplace template.
/// </summary>
public class MarketplacePurchase : BaseEntity
{
    public Guid MarketplaceTemplateId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PurchasedById { get; set; }
    public Guid CopiedTemplateId { get; set; } // The template copy in the buyer's tenant
    public int PriceKr { get; set; }
    public DateTimeOffset PurchasedAt { get; set; }

    // Navigation
    public MarketplaceTemplate MarketplaceTemplate { get; set; } = null!;
}
