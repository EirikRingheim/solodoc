using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Documents;

public class BusinessDocument : TenantScopedEntity
{
    public BusinessDocumentType DocumentType { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Published
    public string? ContentJson { get; set; }
    public string? GeneratedPdfKey { get; set; }
    public DateTimeOffset? GeneratedAt { get; set; }
    public Guid CreatedById { get; set; }

    // Navigation
    public ICollection<WasteDisposalEntry> WasteEntries { get; set; } = [];
}
