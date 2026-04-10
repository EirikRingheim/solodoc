using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Employees;

public class InternalTraining : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public Guid TrainerId { get; set; }
    public Guid TraineeId { get; set; }
    public DateOnly Date { get; set; }
    public decimal? DurationHours { get; set; }
    public string? Notes { get; set; }
    public string? SignatureFileKey { get; set; }
}
