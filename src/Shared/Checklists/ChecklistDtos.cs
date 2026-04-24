namespace Solodoc.Shared.Checklists;

// ─── Template List ────────────────────────────────────
public record ChecklistTemplateListItemDto(
    Guid Id,
    string Name,
    string DocumentType,
    string? DocumentNumber,
    int CurrentVersion,
    bool IsPublished,
    string? Tags,
    string? Category,
    bool IsBaseTemplate);

// ─── Template Detail ──────────────────────────────────
public record ChecklistTemplateDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? DocumentNumber,
    string? Category,
    int CurrentVersion,
    bool IsPublished,
    bool RequireSignature,
    int SignatureCount,
    string? SignatureRoles,
    string? Tags,
    bool IsLocked,
    List<ChecklistTemplateItemDto> Items,
    List<TemplateVersionSummaryDto> Versions);

public record TemplateVersionSummaryDto(
    Guid Id,
    int VersionNumber,
    DateTimeOffset? PublishedAt,
    string? PublishedByName);

public record ChecklistTemplateItemDto(
    Guid Id,
    string Type,
    string Label,
    bool IsRequired,
    string? HelpText,
    string? SectionGroup,
    int SortOrder,
    string? DropdownOptions,
    string? UnitLabel,
    bool RequireCommentOnIrrelevant,
    bool AllowPhoto,
    bool AllowComment,
    string Source);

// ─── Template Create/Update ──────────────────────────
public record CreateChecklistTemplateRequest(
    string Name,
    string? Description,
    string? Category,
    string DocumentType = "Checklist",
    bool RequireSignature = true,
    int SignatureCount = 1,
    string? SignatureRoles = null);

public record UpdateChecklistTemplateRequest(
    string Name,
    string? Description,
    string? Category,
    bool RequireSignature,
    int SignatureCount,
    string? SignatureRoles,
    string? Tags);

public record AddTemplateItemRequest(
    string Type,
    string Label,
    bool IsRequired,
    string? HelpText,
    string? SectionGroup,
    int SortOrder,
    string? DropdownOptions,
    string? UnitLabel,
    bool RequireCommentOnIrrelevant = false,
    bool AllowPhoto = false,
    bool AllowComment = false);

public record UpdateTemplateItemRequest(
    string Label,
    bool IsRequired,
    string? HelpText,
    string? SectionGroup,
    int SortOrder,
    string? DropdownOptions,
    string? UnitLabel,
    bool RequireCommentOnIrrelevant,
    bool AllowPhoto,
    bool AllowComment);

// ─── Item Reorder ─────────────────────────────────────
public record ReorderItemRequest(Guid ItemId, int SortOrder);

// ─── Instance List ────────────────────────────────────
public record ChecklistInstanceListItemDto(
    Guid Id,
    string TemplateName,
    string? DocumentNumber,
    string DocumentType,
    string Status,
    Guid? ProjectId,
    string? ProjectName,
    string? LocationIdentifier,
    DateTimeOffset CreatedAt,
    string StartedBy,
    int ItemCount,
    int CompletedItemCount,
    Guid? GroupId,
    string? GroupPrefix,
    int? GroupIndex,
    Guid? ChecklistObjectId = null,
    string? ObjectDisplayName = null);

// ─── Instance Detail ──────────────────────────────────
public record ChecklistInstanceDetailDto(
    Guid Id,
    string TemplateName,
    string? DocumentNumber,
    string Status,
    string? ProjectName,
    string? LocationIdentifier,
    DateTimeOffset CreatedAt,
    string StartedBy,
    DateTimeOffset? SubmittedAt,
    string? SubmittedBy,
    DateTimeOffset? ApprovedAt,
    string? ApprovedBy,
    bool IsReopened,
    string? ReopenedReason,
    string DocumentType,
    List<ChecklistInstanceItemDto> Items);

public record ChecklistInstanceItemDto(
    Guid Id,
    Guid TemplateItemId,
    string Type,
    string Label,
    bool IsRequired,
    string? HelpText,
    string? SectionGroup,
    int SortOrder,
    string? DropdownOptions,
    string? UnitLabel,
    bool RequireCommentOnIrrelevant,
    bool AllowPhoto,
    bool AllowComment,
    // Response values
    string? Value,
    bool? CheckValue,
    bool IsIrrelevant,
    string? IrrelevantComment,
    string? Comment,
    string? PhotoFileKey,
    string? SignatureFileKey,
    DateTimeOffset? CompletedAt);

// ─── Instance Actions ─────────────────────────────────
public record CreateChecklistInstanceRequest(
    Guid TemplateId,
    Guid? ProjectId,
    Guid? JobId,
    string? LocationIdentifier,
    Guid? LocationId = null);

public record SubmitChecklistItemRequest(
    string? Value,
    bool? CheckValue,
    bool IsIrrelevant = false,
    string? IrrelevantComment = null,
    string? Comment = null,
    string? PhotoFileKey = null,
    string? SignatureFileKey = null);

public record ReopenInstanceRequest(string Reason);

public record DuplicateInstanceRequest(string? LocationIdentifier);

// ─── Batch Duplication ────────────────────────────────
public record BatchDuplicateRequest(
    Guid TemplateId,
    Guid ProjectId,
    string Prefix,
    int Count,
    int StartAt = 1);

public record BatchDuplicateResponse(
    Guid GroupId,
    string GroupName,
    List<DuplicateItemInfo> Items);

public record DuplicateItemInfo(Guid InstanceId, string Name, int Index);

// ─── Instance Group ───────────────────────────────────
public record ChecklistInstanceGroupDto(
    Guid GroupId,
    string GroupName,
    string TemplateName,
    string? ProjectName,
    string Prefix,
    int StartIndex,
    int EndIndex,
    int TotalCount,
    int CompletedCount,
    List<ChecklistInstanceListItemDto> Items);

// ─── Participants ─────────────────────────────────────
public record ChecklistParticipantDto(
    Guid Id,
    string PersonName,
    bool HasSigned,
    bool IsExternal = false,
    string? ExternalPhone = null,
    string? ExternalCompany = null);

public record AddChecklistParticipantRequest(Guid PersonId);

public record AddExternalChecklistParticipantRequest(
    string Name,
    string? Phone,
    string? Company);

// ─── Procedure Read Tracking ──────────────────────────
public record ProcedureReadStatusDto(
    int TotalWorkers,
    int ReadCount,
    List<ProcedureReaderDto> Readers);

public record ProcedureReaderDto(string PersonName, DateTimeOffset ReadAt);

public record MarkProcedureReadRequest(Guid ProcedureTemplateId);
