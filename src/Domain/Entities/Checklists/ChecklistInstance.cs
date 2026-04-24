using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Checklists;

public class ChecklistInstance : TenantScopedEntity
{
    public Guid TemplateVersionId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? LocationId { get; set; }
    public ChecklistInstanceStatus Status { get; set; } = ChecklistInstanceStatus.Draft;
    public Guid StartedById { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public Guid? SubmittedById { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ReopenedAt { get; set; }
    public Guid? ReopenedById { get; set; }
    public string? ReopenedReason { get; set; }
    public string? OriginalSnapshotJson { get; set; }
    public string? LocationIdentifier { get; set; }
    public Guid? GroupId { get; set; }         // Links duplicates together
    public string? GroupPrefix { get; set; }    // e.g. "Fundament"
    public int? GroupIndex { get; set; }        // e.g. 1, 2, 3...
    public Guid? ChecklistObjectId { get; set; } // Links to a named object

    // Navigation properties
    public ChecklistTemplateVersion TemplateVersion { get; set; } = null!;
    public ChecklistObject? ChecklistObject { get; set; }
    public ICollection<ChecklistInstanceItem> Items { get; set; } = new List<ChecklistInstanceItem>();
    public ICollection<ChecklistParticipant> Participants { get; set; } = [];
}
