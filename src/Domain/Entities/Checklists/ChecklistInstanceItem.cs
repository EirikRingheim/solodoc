using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Checklists;

public class ChecklistInstanceItem : BaseEntity
{
    public Guid InstanceId { get; set; }
    public Guid TemplateItemId { get; set; }
    public string? Value { get; set; }
    public bool? CheckValue { get; set; }
    public bool IsIrrelevant { get; set; }
    public string? IrrelevantComment { get; set; }
    public string? Comment { get; set; }
    public string? PhotoFileKey { get; set; }
    public string? SignatureFileKey { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    // Navigation properties
    public ChecklistInstance Instance { get; set; } = null!;
}
