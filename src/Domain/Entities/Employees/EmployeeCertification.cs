using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Employees;

public class EmployeeCertification : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? IssuedBy { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? FileKey { get; set; }
    public string? ThumbnailKey { get; set; }
    public string? OcrStatus { get; set; }
    public DateOnly? OcrExtractedExpiry { get; set; }
    public string? Notes { get; set; }

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);

    public bool IsExpiringSoon => ExpiryDate.HasValue
        && !IsExpired
        && ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
}
