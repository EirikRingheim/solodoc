using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Employees;

public class SickLeaveEntry : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid TenantId { get; set; }
    public SickLeaveType Type { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Days { get; set; }
    public string? Notes { get; set; }
}
