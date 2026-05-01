using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Checklists;

public class ChecklistTemplateItem : BaseEntity
{
    public Guid TemplateVersionId { get; set; }
    public ChecklistItemType Type { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? HelpText { get; set; }
    public string? SectionGroup { get; set; }
    public int SortOrder { get; set; }
    public string? DropdownOptions { get; set; }
    public string? UnitLabel { get; set; }
    public bool RequireCommentOnIrrelevant { get; set; }
    public bool AllowPhoto { get; set; }
    public bool AllowComment { get; set; }
    public string Source { get; set; } = "tenant";
    public Guid? ParentItemId { get; set; }
    public int? SubNumber { get; set; } // For 1.1, 1.2 numbering

    // Navigation properties
    public ChecklistTemplateVersion TemplateVersion { get; set; } = null!;
}
