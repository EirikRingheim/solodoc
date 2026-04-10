using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Deviations;

public class DeviationPhoto : BaseEntity
{
    public Guid DeviationId { get; set; }
    public string FileKey { get; set; } = string.Empty;
    public string? ThumbnailKey { get; set; }
    public bool IsAnnotated { get; set; }
    public bool IsBeforePhoto { get; set; } = true;
    public string? AnnotatedFileKey { get; set; }
    public int SortOrder { get; set; }

    public Deviation Deviation { get; set; } = null!;
}
