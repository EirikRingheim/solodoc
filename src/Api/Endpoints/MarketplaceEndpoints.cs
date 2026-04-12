using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Checklists;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Shared.Marketplace;

namespace Solodoc.Api.Endpoints;

public static class MarketplaceEndpoints
{
    public static WebApplication MapMarketplaceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/marketplace", ListMarketplaceTemplates).RequireAuthorization();
        app.MapPost("/api/marketplace/publish/{templateId:guid}", PublishToMarketplace).RequireAuthorization();
        app.MapPost("/api/marketplace/buy/{id:guid}", BuyTemplate).RequireAuthorization();
        app.MapGet("/api/marketplace/purchases", GetMyPurchases).RequireAuthorization();
        app.MapPut("/api/marketplace/{id:guid}", UpdateMarketplaceTemplate).RequireAuthorization();
        app.MapDelete("/api/marketplace/{id:guid}", UnpublishFromMarketplace).RequireAuthorization();
        app.MapPost("/api/marketplace/{id:guid}/republish", RepublishTemplate).RequireAuthorization();

        return app;
    }

    // ─── List marketplace templates ─────────────────────

    private static async Task<IResult> ListMarketplaceTemplates(
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct,
        bool includeHidden = false)
    {
        var query = db.MarketplaceTemplates.Where(m => !m.IsDeleted);
        if (!includeHidden)
            query = query.Where(m => m.IsPublished);

        var templates = await query
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .Select(m => new MarketplaceListItemDto(
                m.Id, m.Name, m.Description, m.Category, m.Color,
                m.ItemCount, m.PriceKr, m.PurchaseCount, false, !m.IsPublished))
            .ToListAsync(ct);

        // Mark which ones this tenant already purchased
        if (tp.TenantId.HasValue)
        {
            var purchased = await db.MarketplacePurchases
                .Where(p => p.TenantId == tp.TenantId.Value && !p.IsDeleted)
                .Select(p => p.MarketplaceTemplateId)
                .ToListAsync(ct);

            return Results.Ok(templates.Select(t => t with { AlreadyPurchased = purchased.Contains(t.Id) }).ToList());
        }

        return Results.Ok(templates);
    }

    // ─── Publish template to marketplace (super admin) ──

    private static async Task<IResult> PublishToMarketplace(
        Guid templateId,
        PublishToMarketplaceRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var template = await db.ChecklistTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId && t.TenantId == tp.TenantId.Value, ct);
        if (template is null) return Results.NotFound();

        // Count items
        var latestVersion = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == templateId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        var itemCount = latestVersion is not null
            ? await db.ChecklistTemplateItems.CountAsync(i => i.TemplateVersionId == latestVersion.Id, ct)
            : 0;

        // Check if already published
        var existing = await db.MarketplaceTemplates
            .FirstOrDefaultAsync(m => m.SourceTemplateId == templateId && !m.IsDeleted, ct);

        if (existing is not null)
        {
            existing.Name = request.Name ?? template.Name;
            existing.Description = request.Description ?? template.Description;
            existing.Category = request.Category ?? "Generell";
            existing.Color = request.Color;
            existing.ItemCount = itemCount;
            existing.PriceKr = request.PriceKr ?? 49;
            existing.IsPublished = true;
        }
        else
        {
            var mp = new MarketplaceTemplate
            {
                SourceTemplateId = templateId,
                SourceTenantId = tp.TenantId.Value,
                Name = request.Name ?? template.Name,
                Description = request.Description ?? template.Description,
                Category = request.Category ?? "Generell",
                Color = request.Color,
                ItemCount = itemCount,
                PriceKr = request.PriceKr ?? 49,
                IsPublished = true
            };
            db.MarketplaceTemplates.Add(mp);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ─── Buy template ───────────────────────────────────

    private static async Task<IResult> BuyTemplate(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();
        var tenantId = tp.TenantId.Value;

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId)) return Results.Unauthorized();

        var mp = await db.MarketplaceTemplates
            .FirstOrDefaultAsync(m => m.Id == id && m.IsPublished && !m.IsDeleted, ct);
        if (mp is null) return Results.NotFound();

        // Check if already purchased
        var alreadyBought = await db.MarketplacePurchases
            .AnyAsync(p => p.MarketplaceTemplateId == id && p.TenantId == tenantId && !p.IsDeleted, ct);
        if (alreadyBought)
            return Results.BadRequest(new { error = "Du har allerede kjøpt denne malen." });

        // Copy the source template to buyer's tenant
        var source = await db.ChecklistTemplates
            .FirstOrDefaultAsync(t => t.Id == mp.SourceTemplateId, ct);
        if (source is null) return Results.NotFound();

        var copy = new ChecklistTemplate
        {
            TenantId = tenantId,
            Name = mp.Name,
            Description = mp.Description,
            Category = source.Category,
            DocumentType = source.DocumentType,
            DocumentNumber = $"MKT-{mp.Id.ToString()[..4].ToUpper()}",
            RequireSignature = source.RequireSignature,
            SignatureCount = source.SignatureCount,
            SignatureRoles = source.SignatureRoles,
            Tags = source.Tags,
            CurrentVersion = 1,
            IsPublished = true
        };
        db.ChecklistTemplates.Add(copy);

        var newVersion = new ChecklistTemplateVersion
        {
            ChecklistTemplateId = copy.Id,
            VersionNumber = 1,
            PublishedAt = DateTimeOffset.UtcNow,
            PublishedById = personId
        };
        db.ChecklistTemplateVersions.Add(newVersion);

        // Copy items from source
        var sourceVersion = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == mp.SourceTemplateId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (sourceVersion is not null)
        {
            var items = await db.ChecklistTemplateItems
                .Where(i => i.TemplateVersionId == sourceVersion.Id)
                .ToListAsync(ct);

            foreach (var item in items)
            {
                db.ChecklistTemplateItems.Add(new ChecklistTemplateItem
                {
                    TemplateVersionId = newVersion.Id,
                    Type = item.Type,
                    Label = item.Label,
                    IsRequired = item.IsRequired,
                    HelpText = item.HelpText,
                    SectionGroup = item.SectionGroup,
                    SortOrder = item.SortOrder,
                    DropdownOptions = item.DropdownOptions,
                    UnitLabel = item.UnitLabel,
                    RequireCommentOnIrrelevant = item.RequireCommentOnIrrelevant,
                    AllowPhoto = item.AllowPhoto,
                    AllowComment = item.AllowComment,
                    Source = "Marketplace"
                });
            }
        }

        // Record purchase
        var purchase = new MarketplacePurchase
        {
            MarketplaceTemplateId = id,
            TenantId = tenantId,
            PurchasedById = personId,
            CopiedTemplateId = copy.Id,
            PriceKr = mp.PriceKr,
            PurchasedAt = DateTimeOffset.UtcNow
        };
        db.MarketplacePurchases.Add(purchase);

        mp.PurchaseCount++;

        await db.SaveChangesAsync(ct);

        return Results.Ok(new { templateId = copy.Id, price = mp.PriceKr });
    }

    // ─── My purchases ───────────────────────────────────

    private static async Task<IResult> GetMyPurchases(
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var purchases = await db.MarketplacePurchases
            .Where(p => p.TenantId == tp.TenantId.Value && !p.IsDeleted)
            .OrderByDescending(p => p.PurchasedAt)
            .Select(p => new PurchaseDto(
                p.Id, p.MarketplaceTemplate.Name, p.CopiedTemplateId,
                p.PriceKr, p.PurchasedAt))
            .ToListAsync(ct);

        return Results.Ok(purchases);
    }

    private static bool IsSuperAdmin(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        return roles.Contains("SuperAdmin") || roles.Contains("super-admin");
    }

    // ─── Update marketplace template ─────────────────────

    private static async Task<IResult> UpdateMarketplaceTemplate(
        Guid id,
        PublishToMarketplaceRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();
        var mp = await db.MarketplaceTemplates.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
        if (mp is null) return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Name)) mp.Name = request.Name;
        if (request.Description is not null) mp.Description = request.Description;
        if (!string.IsNullOrWhiteSpace(request.Category)) mp.Category = request.Category;
        if (request.Color is not null) mp.Color = request.Color;
        if (request.PriceKr.HasValue) mp.PriceKr = request.PriceKr.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ─── Republish (super admin) ────────────────────────

    private static async Task<IResult> RepublishTemplate(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();
        var mp = await db.MarketplaceTemplates.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
        if (mp is null) return Results.NotFound();

        mp.IsPublished = true;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ─── Unpublish (super admin) ────────────────────────

    private static async Task<IResult> UnpublishFromMarketplace(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (!IsSuperAdmin(user)) return Results.Forbid();
        var mp = await db.MarketplaceTemplates.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (mp is null) return Results.NotFound();

        mp.IsPublished = false;
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
