using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Billing;

public class ClientError : BaseEntity
{
    public Guid? TenantId { get; set; }
    public Guid? PersonId { get; set; }
    public string? UserEmail { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Page { get; set; }
    public string? UserAgent { get; set; }
    public string? AdditionalInfo { get; set; }
    public bool IsResolved { get; set; }
}
