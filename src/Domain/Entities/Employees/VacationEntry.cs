using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Employees;

public class VacationEntry : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid TenantId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Days { get; set; }
    public VacationStatus Status { get; set; } = VacationStatus.Pending;
    public Guid? ApprovedById { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}
