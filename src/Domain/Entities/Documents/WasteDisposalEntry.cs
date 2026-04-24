using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Documents;

public class WasteDisposalEntry : BaseEntity
{
    public Guid BusinessDocumentId { get; set; }
    public WasteCategory Category { get; set; }
    public string? Description { get; set; }
    public decimal? WeightKg { get; set; }
    public DateOnly DisposedAt { get; set; }
    public string? DisposalMethod { get; set; }
    public string? ReceiptFileKey { get; set; }

    // Navigation
    public BusinessDocument Document { get; set; } = null!;
}
