using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Projects;

public class JobPartsItem : BaseEntity
{
    public Guid JobId { get; set; }
    public string Description { get; set; } = string.Empty;
    public PartsItemStatus Status { get; set; } = PartsItemStatus.Trengs;
    public string? Notes { get; set; }
    public Guid AddedById { get; set; }

    // Navigation
    public Job Job { get; set; } = null!;
}
