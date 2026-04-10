using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Solodoc.Application.Common;
using Solodoc.Domain.Entities.Checklists;
using Solodoc.Domain.Enums;
using Solodoc.Infrastructure.Persistence;
using Solodoc.Infrastructure.Services;
using Solodoc.Shared.Checklists;

namespace Solodoc.Api.Endpoints;

public static class ChecklistEndpoints
{
    public static WebApplication MapChecklistEndpoints(this WebApplication app)
    {
        // Templates
        app.MapGet("/api/checklists/templates", ListTemplates).RequireAuthorization();
        app.MapPost("/api/checklists/templates", CreateTemplate).RequireAuthorization();
        app.MapGet("/api/checklists/templates/{id:guid}", GetTemplateDetail).RequireAuthorization();
        app.MapPut("/api/checklists/templates/{id:guid}", UpdateTemplate).RequireAuthorization();
        app.MapPost("/api/checklists/templates/{id:guid}/items", AddTemplateItem).RequireAuthorization();
        app.MapPut("/api/checklists/templates/{id:guid}/items/{itemId:guid}", UpdateTemplateItem).RequireAuthorization();
        app.MapDelete("/api/checklists/templates/{id:guid}/items/{itemId:guid}", DeleteTemplateItem).RequireAuthorization();
        app.MapPost("/api/checklists/templates/{id:guid}/publish", PublishTemplate).RequireAuthorization();
        app.MapPost("/api/checklists/templates/{id:guid}/duplicate", DuplicateTemplate).RequireAuthorization();
        app.MapPut("/api/checklists/templates/{id:guid}/items/reorder", ReorderTemplateItems).RequireAuthorization();
        app.MapPost("/api/checklists/templates/import", ImportTemplate).RequireAuthorization().DisableAntiforgery();

        // Instances
        app.MapGet("/api/checklists/instances", ListInstances).RequireAuthorization();
        app.MapGet("/api/checklists/instances/{id:guid}", GetInstanceDetail).RequireAuthorization();
        app.MapPost("/api/checklists/instances", CreateInstance).RequireAuthorization();
        app.MapPut("/api/checklists/instances/{id:guid}/items/{itemId:guid}", SubmitItem).RequireAuthorization();
        app.MapPatch("/api/checklists/instances/{id:guid}/submit", SubmitInstance).RequireAuthorization();
        app.MapPatch("/api/checklists/instances/{id:guid}/approve", ApproveInstance).RequireAuthorization();
        app.MapPatch("/api/checklists/instances/{id:guid}/reopen", ReopenInstance).RequireAuthorization();
        app.MapPost("/api/checklists/instances/{id:guid}/duplicate", DuplicateInstance).RequireAuthorization();
        app.MapPost("/api/checklists/instances/batch-duplicate", BatchDuplicate).RequireAuthorization();
        app.MapDelete("/api/checklists/instances/{id:guid}", DeleteInstance).RequireAuthorization();

        return app;
    }

    private static Guid? GetPersonId(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    // ─── Template Endpoints ──────────────────────────────

    private static async Task<IResult> ListTemplates(
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var templates = await db.ChecklistTemplates
            .Where(t => t.TenantId == tenantProvider.TenantId.Value && !t.IsArchived)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new ChecklistTemplateListItemDto(
                t.Id,
                t.Name,
                t.DocumentType,
                t.DocumentNumber,
                t.CurrentVersion,
                t.IsPublished,
                t.Tags,
                t.Category,
                t.IsBaseTemplate))
            .ToListAsync(ct);

        return Results.Ok(templates);
    }

    private static async Task<IResult> CreateTemplate(
        CreateChecklistTemplateRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Results.BadRequest(new { error = "Navn er påkrevd." });

        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;

        // Auto-generate document number
        var currentMax = await db.ChecklistTemplates
            .Where(t => t.TenantId == tenantId && t.DocumentNumber != null)
            .CountAsync(ct);
        var seq = currentMax + 1;
        var documentNumber = $"SJL-{seq:D3}";

        // Auto-tag from name
        var tags = AutoTagger.ExtractTags(request.Name, Enumerable.Empty<string>());

        var template = new ChecklistTemplate
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            DocumentType = request.DocumentType,
            DocumentNumber = documentNumber,
            RequireSignature = request.RequireSignature,
            SignatureCount = request.SignatureCount,
            SignatureRoles = request.SignatureRoles,
            Tags = tags,
            CurrentVersion = 1,
            IsPublished = false
        };

        db.ChecklistTemplates.Add(template);

        // Create the initial version (draft)
        var version = new ChecklistTemplateVersion
        {
            ChecklistTemplateId = template.Id,
            VersionNumber = 1,
            PublishedAt = default,
            PublishedById = Guid.Empty
        };

        db.ChecklistTemplateVersions.Add(version);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/checklists/templates/{template.Id}", new { id = template.Id });
    }

    private static async Task<IResult> GetTemplateDetail(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var template = await db.ChecklistTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantProvider.TenantId.Value, ct);

        if (template is null)
            return Results.NotFound();

        // Get latest version (published preferred, otherwise latest draft)
        var version = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id && v.PublishedAt != default)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        version ??= await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        var items = version is not null
            ? await db.ChecklistTemplateItems
                .Where(i => i.TemplateVersionId == version.Id)
                .OrderBy(i => i.SortOrder)
                .Select(i => new ChecklistTemplateItemDto(
                    i.Id,
                    i.Type.ToString(),
                    i.Label,
                    i.IsRequired,
                    i.HelpText,
                    i.SectionGroup,
                    i.SortOrder,
                    i.DropdownOptions,
                    i.UnitLabel,
                    i.RequireCommentOnIrrelevant,
                    i.AllowPhoto,
                    i.AllowComment,
                    i.Source))
                .ToListAsync(ct)
            : new List<ChecklistTemplateItemDto>();

        // Version history
        var versions = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new TemplateVersionSummaryDto(
                v.Id,
                v.VersionNumber,
                v.PublishedAt == default ? null : v.PublishedAt,
                v.PublishedById != Guid.Empty
                    ? db.Persons.Where(p => p.Id == v.PublishedById).Select(p => p.FullName).FirstOrDefault()
                    : null))
            .ToListAsync(ct);

        return Results.Ok(new ChecklistTemplateDetailDto(
            template.Id,
            template.Name,
            template.Description,
            template.DocumentNumber,
            template.Category,
            template.CurrentVersion,
            template.IsPublished,
            template.RequireSignature,
            template.SignatureCount,
            template.SignatureRoles,
            template.Tags,
            template.IsLocked,
            items,
            versions));
    }

    private static async Task<IResult> UpdateTemplate(
        Guid id,
        UpdateChecklistTemplateRequest request,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var template = await db.ChecklistTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template is null)
            return Results.NotFound();

        if (template.IsLocked)
            return Results.BadRequest(new { error = "Malen er låst og kan ikke endres." });

        template.Name = request.Name;
        template.Description = request.Description;
        template.Category = request.Category;
        template.RequireSignature = request.RequireSignature;
        template.SignatureCount = request.SignatureCount;
        template.SignatureRoles = request.SignatureRoles;
        if (request.Tags is not null)
            template.Tags = request.Tags;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> AddTemplateItem(
        Guid id,
        AddTemplateItemRequest request,
        SolodocDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
            return Results.BadRequest(new { error = "Label er påkrevd." });

        // Find the current (latest) version for this template
        var version = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (version is null)
            return Results.NotFound();

        if (!Enum.TryParse<ChecklistItemType>(request.Type, true, out var itemType))
            return Results.BadRequest(new { error = $"Ugyldig item type: {request.Type}" });

        var item = new ChecklistTemplateItem
        {
            TemplateVersionId = version.Id,
            Type = itemType,
            Label = request.Label,
            IsRequired = request.IsRequired,
            HelpText = request.HelpText,
            SectionGroup = request.SectionGroup,
            SortOrder = request.SortOrder,
            DropdownOptions = request.DropdownOptions,
            UnitLabel = request.UnitLabel,
            RequireCommentOnIrrelevant = request.RequireCommentOnIrrelevant,
            AllowPhoto = request.AllowPhoto,
            AllowComment = request.AllowComment
        };

        db.ChecklistTemplateItems.Add(item);

        // Re-generate tags for the template
        var template = await db.ChecklistTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (template is not null)
        {
            var allLabels = await db.ChecklistTemplateItems
                .Where(i => i.TemplateVersionId == version.Id)
                .Select(i => i.Label)
                .ToListAsync(ct);
            allLabels.Add(request.Label);
            template.Tags = AutoTagger.ExtractTags(template.Name, allLabels);
        }

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/checklists/templates/{id}/items/{item.Id}", new { id = item.Id });
    }

    private static async Task<IResult> UpdateTemplateItem(
        Guid id,
        Guid itemId,
        UpdateTemplateItemRequest request,
        SolodocDbContext db,
        CancellationToken ct)
    {
        // Find the latest version for this template
        var version = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (version is null)
            return Results.NotFound();

        var item = await db.ChecklistTemplateItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.TemplateVersionId == version.Id, ct);

        if (item is null)
            return Results.NotFound();

        item.Label = request.Label;
        item.IsRequired = request.IsRequired;
        item.HelpText = request.HelpText;
        item.SectionGroup = request.SectionGroup;
        item.SortOrder = request.SortOrder;
        item.DropdownOptions = request.DropdownOptions;
        item.UnitLabel = request.UnitLabel;
        item.RequireCommentOnIrrelevant = request.RequireCommentOnIrrelevant;
        item.AllowPhoto = request.AllowPhoto;
        item.AllowComment = request.AllowComment;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteTemplateItem(
        Guid id,
        Guid itemId,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var version = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (version is null)
            return Results.NotFound();

        var item = await db.ChecklistTemplateItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.TemplateVersionId == version.Id, ct);

        if (item is null)
            return Results.NotFound();

        item.IsDeleted = true;
        item.DeletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> PublishTemplate(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var template = await db.ChecklistTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (template is null)
            return Results.NotFound();

        // Mark current version as published
        var currentVersion = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (currentVersion is null)
            return Results.NotFound();

        currentVersion.PublishedAt = DateTimeOffset.UtcNow;
        currentVersion.PublishedById = personId.Value;

        template.IsPublished = true;

        // Increment version counter and create a new draft version
        template.CurrentVersion += 1;

        var newVersion = new ChecklistTemplateVersion
        {
            ChecklistTemplateId = template.Id,
            VersionNumber = template.CurrentVersion,
            PublishedAt = default,
            PublishedById = Guid.Empty
        };

        // Copy items from current version to new version
        var existingItems = await db.ChecklistTemplateItems
            .Where(i => i.TemplateVersionId == currentVersion.Id)
            .ToListAsync(ct);

        db.ChecklistTemplateVersions.Add(newVersion);

        foreach (var existingItem in existingItems)
        {
            var copiedItem = new ChecklistTemplateItem
            {
                TemplateVersionId = newVersion.Id,
                Type = existingItem.Type,
                Label = existingItem.Label,
                IsRequired = existingItem.IsRequired,
                HelpText = existingItem.HelpText,
                SectionGroup = existingItem.SectionGroup,
                SortOrder = existingItem.SortOrder,
                DropdownOptions = existingItem.DropdownOptions,
                UnitLabel = existingItem.UnitLabel,
                RequireCommentOnIrrelevant = existingItem.RequireCommentOnIrrelevant,
                AllowPhoto = existingItem.AllowPhoto,
                AllowComment = existingItem.AllowComment,
                Source = existingItem.Source
            };
            db.ChecklistTemplateItems.Add(copiedItem);
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new { version = currentVersion.VersionNumber });
    }

    private static async Task<IResult> DuplicateTemplate(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var tenantId = tenantProvider.TenantId.Value;

        var original = await db.ChecklistTemplates
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId, ct);

        if (original is null)
            return Results.NotFound();

        // Auto-generate new document number
        var currentMax = await db.ChecklistTemplates
            .Where(t => t.TenantId == tenantId && t.DocumentNumber != null)
            .CountAsync(ct);
        var seq = currentMax + 1;

        var duplicate = new ChecklistTemplate
        {
            TenantId = tenantId,
            Name = $"{original.Name} (Kopi)",
            Description = original.Description,
            Category = original.Category,
            DocumentType = original.DocumentType,
            DocumentNumber = $"SJL-{seq:D3}",
            RequireSignature = original.RequireSignature,
            SignatureCount = original.SignatureCount,
            SignatureRoles = original.SignatureRoles,
            Tags = original.Tags,
            CurrentVersion = 1,
            IsPublished = false
        };

        db.ChecklistTemplates.Add(duplicate);

        var newVersion = new ChecklistTemplateVersion
        {
            ChecklistTemplateId = duplicate.Id,
            VersionNumber = 1,
            PublishedAt = default,
            PublishedById = Guid.Empty
        };
        db.ChecklistTemplateVersions.Add(newVersion);

        // Copy items from the latest version of the original
        var latestVersion = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (latestVersion is not null)
        {
            var items = await db.ChecklistTemplateItems
                .Where(i => i.TemplateVersionId == latestVersion.Id)
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
                    Source = item.Source
                });
            }
        }

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/checklists/templates/{duplicate.Id}", new { id = duplicate.Id });
    }

    private static async Task<IResult> ReorderTemplateItems(
        Guid id,
        List<ReorderItemRequest> request,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var version = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == id)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (version is null)
            return Results.NotFound();

        var items = await db.ChecklistTemplateItems
            .Where(i => i.TemplateVersionId == version.Id)
            .ToListAsync(ct);

        foreach (var reorder in request)
        {
            var item = items.FirstOrDefault(i => i.Id == reorder.ItemId);
            if (item is not null)
                item.SortOrder = reorder.SortOrder;
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    // ─── Instance Endpoints ──────────────────────────────

    private static async Task<IResult> ListInstances(
        ITenantProvider tenantProvider,
        SolodocDbContext db,
        Guid? projectId,
        Guid? locationId,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var query = db.ChecklistInstances
            .Where(i => i.TenantId == tenantProvider.TenantId.Value);

        if (projectId.HasValue)
            query = query.Where(i => i.ProjectId == projectId.Value);

        if (locationId.HasValue)
            query = query.Where(i => i.LocationId == locationId.Value);

        var instances = await query
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new ChecklistInstanceListItemDto(
                i.Id,
                db.ChecklistTemplateVersions
                    .Where(v => v.Id == i.TemplateVersionId)
                    .Select(v => v.ChecklistTemplate.Name)
                    .FirstOrDefault() ?? "",
                db.ChecklistTemplateVersions
                    .Where(v => v.Id == i.TemplateVersionId)
                    .Select(v => v.ChecklistTemplate.DocumentNumber)
                    .FirstOrDefault(),
                db.ChecklistTemplateVersions
                    .Where(v => v.Id == i.TemplateVersionId)
                    .Select(v => v.ChecklistTemplate.DocumentType)
                    .FirstOrDefault() ?? "Checklist",
                FormatStatus(i.Status),
                i.ProjectId,
                i.ProjectId != null
                    ? db.Projects.Where(p => p.Id == i.ProjectId).Select(p => p.Name).FirstOrDefault()
                    : null,
                i.LocationIdentifier,
                i.CreatedAt,
                db.Persons.Where(p => p.Id == i.StartedById).Select(p => p.FullName).FirstOrDefault() ?? "",
                db.ChecklistInstanceItems.Count(ii => ii.InstanceId == i.Id),
                db.ChecklistInstanceItems.Count(ii => ii.InstanceId == i.Id
                    && (ii.CheckValue != null || ii.Value != null || ii.IsIrrelevant)),
                i.GroupId,
                i.GroupPrefix,
                i.GroupIndex))
            .ToListAsync(ct);

        return Results.Ok(instances);
    }

    private static async Task<IResult> GetInstanceDetail(
        Guid id,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var instance = await db.ChecklistInstances
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantProvider.TenantId.Value, ct);

        if (instance is null)
            return Results.NotFound();

        var templateName = await db.ChecklistTemplateVersions
            .Where(v => v.Id == instance.TemplateVersionId)
            .Select(v => v.ChecklistTemplate.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        var documentNumber = await db.ChecklistTemplateVersions
            .Where(v => v.Id == instance.TemplateVersionId)
            .Select(v => v.ChecklistTemplate.DocumentNumber)
            .FirstOrDefaultAsync(ct);

        var projectName = instance.ProjectId != null
            ? await db.Projects.Where(p => p.Id == instance.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(ct)
            : null;

        var startedByName = await db.Persons
            .Where(p => p.Id == instance.StartedById).Select(p => p.FullName).FirstOrDefaultAsync(ct) ?? "";

        var submittedByName = instance.SubmittedById != null
            ? await db.Persons.Where(p => p.Id == instance.SubmittedById).Select(p => p.FullName).FirstOrDefaultAsync(ct)
            : null;

        var approvedByName = instance.ApprovedById != null
            ? await db.Persons.Where(p => p.Id == instance.ApprovedById).Select(p => p.FullName).FirstOrDefaultAsync(ct)
            : null;

        // Load instance items and template items separately, then join in memory
        var instanceItems = await db.ChecklistInstanceItems
            .Where(ii => ii.InstanceId == id)
            .Select(ii => new { ii.Id, ii.TemplateItemId, ii.Value, ii.CheckValue,
                ii.IsIrrelevant, ii.IrrelevantComment, ii.Comment,
                ii.PhotoFileKey, ii.SignatureFileKey, ii.CompletedAt })
            .ToListAsync(ct);

        var templateItemIds = instanceItems.Select(ii => ii.TemplateItemId).Distinct().ToList();
        var templateItems = await db.ChecklistTemplateItems
            .Where(ti => templateItemIds.Contains(ti.Id))
            .ToDictionaryAsync(ti => ti.Id, ti => ti, ct);

        var items = instanceItems
            .Where(ii => templateItems.ContainsKey(ii.TemplateItemId))
            .Select(ii =>
            {
                var ti = templateItems[ii.TemplateItemId];
                return new ChecklistInstanceItemDto(
                    ii.Id, ii.TemplateItemId,
                    ti.Type.ToString(), ti.Label, ti.IsRequired,
                    ti.HelpText, ti.SectionGroup, ti.SortOrder,
                    ti.DropdownOptions, ti.UnitLabel,
                    ti.RequireCommentOnIrrelevant, ti.AllowPhoto, ti.AllowComment,
                    ii.Value, ii.CheckValue, ii.IsIrrelevant, ii.IrrelevantComment,
                    ii.Comment, ii.PhotoFileKey, ii.SignatureFileKey, ii.CompletedAt);
            })
            .OrderBy(i => i.SortOrder)
            .ToList();

        return Results.Ok(new ChecklistInstanceDetailDto(
            instance.Id,
            templateName,
            documentNumber,
            FormatStatus(instance.Status),
            projectName,
            instance.LocationIdentifier,
            instance.CreatedAt,
            startedByName,
            instance.SubmittedAt,
            submittedByName,
            instance.ApprovedAt,
            approvedByName,
            instance.Status == ChecklistInstanceStatus.Reopened,
            instance.ReopenedReason,
            items));
    }

    private static async Task<IResult> CreateInstance(
        CreateChecklistInstanceRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        // Find the latest published version for the template
        var templateVersion = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == request.TemplateId && v.PublishedAt != default)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (templateVersion is null)
            return Results.BadRequest(new { error = "Ingen publisert versjon funnet for denne malen." });

        var instance = new ChecklistInstance
        {
            TenantId = tenantProvider.TenantId.Value,
            TemplateVersionId = templateVersion.Id,
            ProjectId = request.ProjectId,
            JobId = request.JobId,
            StartedById = personId.Value,
            LocationIdentifier = request.LocationIdentifier,
            LocationId = request.LocationId,
            Status = ChecklistInstanceStatus.Draft
        };

        db.ChecklistInstances.Add(instance);

        // Create instance items from template items
        var templateItems = await db.ChecklistTemplateItems
            .Where(i => i.TemplateVersionId == templateVersion.Id)
            .ToListAsync(ct);

        foreach (var templateItem in templateItems)
        {
            var instanceItem = new ChecklistInstanceItem
            {
                InstanceId = instance.Id,
                TemplateItemId = templateItem.Id
            };
            db.ChecklistInstanceItems.Add(instanceItem);
        }

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/checklists/instances/{instance.Id}", new { id = instance.Id });
    }

    private static async Task<IResult> SubmitItem(
        Guid id,
        Guid itemId,
        SubmitChecklistItemRequest request,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var instanceItem = await db.ChecklistInstanceItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.InstanceId == id, ct);

        if (instanceItem is null)
            return Results.NotFound();

        instanceItem.Value = request.Value;
        instanceItem.CheckValue = request.CheckValue;
        instanceItem.IsIrrelevant = request.IsIrrelevant;
        instanceItem.IrrelevantComment = request.IrrelevantComment;
        instanceItem.Comment = request.Comment;
        if (request.PhotoFileKey is not null) instanceItem.PhotoFileKey = request.PhotoFileKey;
        if (request.SignatureFileKey is not null) instanceItem.SignatureFileKey = request.SignatureFileKey;
        instanceItem.CompletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DeleteInstance(
        Guid id, ClaimsPrincipal user, SolodocDbContext db, ITenantProvider tenantProvider, CancellationToken ct)
    {
        if (tenantProvider.TenantId is null) return Results.Unauthorized();

        var instance = await db.ChecklistInstances
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantProvider.TenantId.Value, ct);
        if (instance is null) return Results.NotFound();
        if (instance.Status != ChecklistInstanceStatus.Draft)
            return Results.BadRequest(new { error = "Kun utkast kan slettes." });

        var items = await db.ChecklistInstanceItems.Where(ii => ii.InstanceId == id).ToListAsync(ct);
        db.ChecklistInstanceItems.RemoveRange(items);
        db.ChecklistInstances.Remove(instance);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SubmitInstance(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var instance = await db.ChecklistInstances
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (instance is null)
            return Results.NotFound();

        if (instance.Status is not (ChecklistInstanceStatus.Draft or ChecklistInstanceStatus.Reopened))
            return Results.BadRequest(new { error = "Sjekklisten kan ikke sendes inn i nåværende status." });

        instance.Status = ChecklistInstanceStatus.Submitted;
        instance.SubmittedAt = DateTimeOffset.UtcNow;
        instance.SubmittedById = personId.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ApproveInstance(
        Guid id,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        var instance = await db.ChecklistInstances
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (instance is null)
            return Results.NotFound();

        if (instance.Status != ChecklistInstanceStatus.Submitted)
            return Results.BadRequest(new { error = "Kun innsendte sjekklister kan godkjennes." });

        instance.Status = ChecklistInstanceStatus.Approved;
        instance.ApprovedAt = DateTimeOffset.UtcNow;
        instance.ApprovedById = personId.Value;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> ReopenInstance(
        Guid id,
        ReopenInstanceRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null)
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Reason))
            return Results.BadRequest(new { error = "Begrunnelse er påkrevd." });

        var instance = await db.ChecklistInstances
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (instance is null)
            return Results.NotFound();

        if (instance.Status is not (ChecklistInstanceStatus.Submitted or ChecklistInstanceStatus.Approved))
            return Results.BadRequest(new { error = "Kun innsendte eller godkjente sjekklister kan gjenåpnes." });

        // Preserve original state as JSON snapshot
        var snapshot = new
        {
            instance.Status,
            instance.SubmittedAt,
            instance.SubmittedById,
            instance.ApprovedAt,
            instance.ApprovedById,
            Items = instance.Items.Select(i => new
            {
                i.Id,
                i.TemplateItemId,
                i.Value,
                i.CheckValue,
                i.IsIrrelevant,
                i.IrrelevantComment,
                i.Comment,
                i.CompletedAt
            })
        };
        instance.OriginalSnapshotJson = JsonSerializer.Serialize(snapshot);

        instance.Status = ChecklistInstanceStatus.Reopened;
        instance.ReopenedAt = DateTimeOffset.UtcNow;
        instance.ReopenedById = personId.Value;
        instance.ReopenedReason = request.Reason;

        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> DuplicateInstance(
        Guid id,
        DuplicateInstanceRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        var original = await db.ChecklistInstances
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (original is null)
            return Results.NotFound();

        var newInstance = new ChecklistInstance
        {
            TenantId = tenantProvider.TenantId.Value,
            TemplateVersionId = original.TemplateVersionId,
            ProjectId = original.ProjectId,
            JobId = original.JobId,
            StartedById = personId.Value,
            LocationIdentifier = request.LocationIdentifier,
            Status = ChecklistInstanceStatus.Draft
        };

        db.ChecklistInstances.Add(newInstance);

        // Create blank instance items from the same template items
        var templateItems = await db.ChecklistTemplateItems
            .Where(i => i.TemplateVersionId == original.TemplateVersionId)
            .ToListAsync(ct);

        foreach (var templateItem in templateItems)
        {
            db.ChecklistInstanceItems.Add(new ChecklistInstanceItem
            {
                InstanceId = newInstance.Id,
                TemplateItemId = templateItem.Id
            });
        }

        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/checklists/instances/{newInstance.Id}", new { id = newInstance.Id });
    }

    private static async Task<IResult> BatchDuplicate(
        BatchDuplicateRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var personId = GetPersonId(user);
        if (personId is null || tenantProvider.TenantId is null)
            return Results.Unauthorized();

        if (request.Count < 1 || request.Count > 100)
            return Results.BadRequest(new { error = "Antall må være mellom 1 og 100." });

        if (string.IsNullOrWhiteSpace(request.Prefix))
            return Results.BadRequest(new { error = "Prefiks er påkrevd." });

        // Find the latest published version for the template
        var templateVersion = await db.ChecklistTemplateVersions
            .Where(v => v.ChecklistTemplateId == request.TemplateId && v.PublishedAt != default)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(ct);

        if (templateVersion is null)
            return Results.BadRequest(new { error = "Ingen publisert versjon funnet for denne malen." });

        var templateName = await db.ChecklistTemplates
            .Where(t => t.Id == request.TemplateId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct) ?? "";

        var projectName = await db.Projects
            .Where(p => p.Id == request.ProjectId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(ct);

        var templateItems = await db.ChecklistTemplateItems
            .Where(i => i.TemplateVersionId == templateVersion.Id)
            .ToListAsync(ct);

        var groupId = Guid.NewGuid();
        var endIndex = request.StartAt + request.Count - 1;
        var groupName = $"{templateName} - {projectName ?? "Uten prosjekt"} - {request.Prefix} {request.StartAt}-{endIndex}";
        var items = new List<DuplicateItemInfo>();

        for (var i = 0; i < request.Count; i++)
        {
            var index = request.StartAt + i;
            var instance = new ChecklistInstance
            {
                TenantId = tenantProvider.TenantId.Value,
                TemplateVersionId = templateVersion.Id,
                ProjectId = request.ProjectId,
                StartedById = personId.Value,
                LocationIdentifier = $"{request.Prefix} {index}",
                Status = ChecklistInstanceStatus.Draft,
                GroupId = groupId,
                GroupPrefix = request.Prefix,
                GroupIndex = index
            };

            db.ChecklistInstances.Add(instance);

            foreach (var templateItem in templateItems)
            {
                db.ChecklistInstanceItems.Add(new ChecklistInstanceItem
                {
                    InstanceId = instance.Id,
                    TemplateItemId = templateItem.Id
                });
            }

            items.Add(new DuplicateItemInfo(instance.Id, $"{templateName} - {request.Prefix} {index}", index));
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new BatchDuplicateResponse(groupId, groupName, items));
    }

    private static string FormatStatus(ChecklistInstanceStatus status) => status switch
    {
        ChecklistInstanceStatus.Draft => "Utkast",
        ChecklistInstanceStatus.Submitted => "Innsendt",
        ChecklistInstanceStatus.Approved => "Godkjent",
        ChecklistInstanceStatus.Reopened => "Gjenåpnet",
        _ => status.ToString()
    };

    // ─── Template Import from File ──────────────────────────

    private static async Task<IResult> ImportTemplate(
        HttpRequest request,
        ClaimsPrincipal user,
        SolodocDbContext db,
        ITenantProvider tp,
        CancellationToken ct)
    {
        if (tp.TenantId is null) return Results.Unauthorized();

        var personIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(personIdClaim, out var personId)) return Results.Unauthorized();

        if (!request.HasFormContentType)
            return Results.BadRequest(new { error = "Forventet fil." });

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "Ingen fil mottatt." });

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext is not ".pdf" and not ".xlsx" and not ".xls" and not ".docx" and not ".doc")
            return Results.BadRequest(new { error = "Ugyldig filformat. Stotter PDF, Excel, Word." });

        // Extract text lines from the file
        var lines = new List<string>();

        try
        {
            using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            ms.Position = 0;

            if (ext == ".pdf")
            {
                using var pdfDoc = UglyToad.PdfPig.PdfDocument.Open(ms);
                foreach (var page in pdfDoc.GetPages())
                {
                    var text = page.Text;
                    lines.AddRange(text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
            }
            else if (ext is ".xlsx" or ".xls")
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook(ms);
                var sheet = workbook.Worksheets.First();
                foreach (var row in sheet.RowsUsed())
                {
                    var cellValues = row.CellsUsed().Select(c => c.GetString()).Where(s => !string.IsNullOrWhiteSpace(s));
                    var line = string.Join(" — ", cellValues);
                    if (!string.IsNullOrWhiteSpace(line))
                        lines.Add(line);
                }
            }
            else if (ext is ".docx" or ".doc")
            {
                // Simple text extraction from docx (ZIP with XML)
                try
                {
                    ms.Position = 0;
                    using var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Read);
                    var docEntry = archive.GetEntry("word/document.xml");
                    if (docEntry is not null)
                    {
                        using var docStream = docEntry.Open();
                        var doc = System.Xml.Linq.XDocument.Load(docStream);
                        var ns = doc.Root?.Name.Namespace ?? System.Xml.Linq.XNamespace.None;
                        var wNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                        var paragraphs = doc.Descendants(System.Xml.Linq.XName.Get("p", wNs));
                        foreach (var p in paragraphs)
                        {
                            var text = string.Join("", p.Descendants(System.Xml.Linq.XName.Get("t", wNs)).Select(t => t.Value));
                            if (!string.IsNullOrWhiteSpace(text))
                                lines.Add(text.Trim());
                        }
                    }
                }
                catch
                {
                    return Results.BadRequest(new { error = "Kunne ikke lese Word-filen." });
                }
            }
        }
        catch
        {
            return Results.BadRequest(new { error = "Kunne ikke lese filen." });
        }

        if (lines.Count == 0)
            return Results.BadRequest(new { error = "Ingen tekst funnet i filen." });

        // Parse lines into template items
        // Heuristic: first line = template name, rest = checklist items
        var templateName = lines.First();
        if (templateName.Length > 200) templateName = templateName[..200];

        // Auto-generate document number
        var currentMax = await db.ChecklistTemplates
            .Where(t => t.TenantId == tp.TenantId.Value && t.DocumentNumber != null)
            .CountAsync(ct);
        var documentNumber = $"IMP-{currentMax + 1:D3}";

        var template = new ChecklistTemplate
        {
            TenantId = tp.TenantId.Value,
            Name = templateName,
            DocumentType = "Checklist",
            DocumentNumber = documentNumber,
            IsPublished = true,
            CurrentVersion = 1
        };
        db.ChecklistTemplates.Add(template);

        var version = new ChecklistTemplateVersion
        {
            ChecklistTemplateId = template.Id,
            VersionNumber = 1,
            PublishedAt = DateTimeOffset.UtcNow,
            PublishedById = personId
        };
        db.ChecklistTemplateVersions.Add(version);

        var sortOrder = 1;
        foreach (var line in lines.Skip(1))
        {
            if (line.Length < 3) continue;
            // Skip lines that look like headers/metadata
            if (line.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || c == ':') && line.Length < 50)
                continue;

            var itemType = GuessItemType(line);

            var item = new ChecklistTemplateItem
            {
                TemplateVersionId = version.Id,
                Label = line.Length > 500 ? line[..500] : line,
                Type = itemType,
                SortOrder = sortOrder++,
                IsRequired = false,
                AllowComment = true,
                AllowPhoto = true,
                RequireCommentOnIrrelevant = true,
                Source = "Import"
            };
            db.ChecklistTemplateItems.Add(item);
        }

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id = template.Id });
    }

    private static ChecklistItemType GuessItemType(string line)
    {
        var lower = line.ToLowerInvariant();

        // Lines with checkbox indicators
        if (lower.StartsWith("[ ]") || lower.StartsWith("☐") || lower.StartsWith("□")
            || lower.Contains("ja/nei") || lower.Contains("ok/") || lower.Contains("sjekk"))
            return ChecklistItemType.Check;

        // Lines asking for numbers/measurements
        if (lower.Contains("antall") || lower.Contains("mengde") || lower.Contains("mal:")
            || lower.Contains("temperatur") || lower.Contains("kg") || lower.Contains("meter"))
            return ChecklistItemType.NumberInput;

        // Lines asking for dates
        if (lower.Contains("dato") || lower.Contains("tidspunkt") || lower.Contains("nar:"))
            return ChecklistItemType.DateInput;

        // Lines asking for signature
        if (lower.Contains("signatur") || lower.Contains("underskrift"))
            return ChecklistItemType.Signature;

        // Lines asking for photo
        if (lower.Contains("bilde") || lower.Contains("foto") || lower.Contains("dokumenter med bilde"))
            return ChecklistItemType.Photo;

        // Default to check item (most common in checklists)
        return ChecklistItemType.Check;
    }
}
