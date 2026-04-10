using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Chemicals;

public class ChemicalSds : BaseEntity
{
    public Guid ChemicalId { get; set; }
    public string FileKey { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateOnly? RevisionDate { get; set; }
    public string? Language { get; set; }

    // Navigation
    public Chemical Chemical { get; set; } = null!;
}
