using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Export;

public class ExportJob : TenantScopedEntity
{
    public string Type { get; set; } = string.Empty; // "project", "employee", "custom"
    public Guid? TargetEntityId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public string OutputMode { get; set; } = "CombinedPdf"; // CombinedPdf, StructuredZip, IndividualFiles
    public string? PhotoOption { get; set; } = "compressed"; // full, compressed, thumbnail, none
    public string? SelectionJson { get; set; } // For custom exports - JSON array of selected items
    public string? ResultFileKey { get; set; } // S3 key for the result
    public string? ResultFileName { get; set; }
    public long? ResultFileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; } // Auto-delete after 7 days
    public Guid RequestedById { get; set; }
    public int? ProgressPercent { get; set; }
}
