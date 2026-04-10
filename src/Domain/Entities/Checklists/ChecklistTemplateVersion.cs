using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Checklists;

public class ChecklistTemplateVersion : BaseEntity
{
    public Guid ChecklistTemplateId { get; set; }
    public int VersionNumber { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public Guid PublishedById { get; set; }

    // Navigation properties
    public ChecklistTemplate ChecklistTemplate { get; set; } = null!;
    public ICollection<ChecklistTemplateItem> Items { get; set; } = new List<ChecklistTemplateItem>();
}
