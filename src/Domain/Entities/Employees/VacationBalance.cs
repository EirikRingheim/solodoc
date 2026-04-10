using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Employees;

public class VacationBalance : TenantScopedEntity
{
    public Guid PersonId { get; set; }
    public int Year { get; set; }
    public decimal AnnualAllowanceDays { get; set; }
    public decimal CarriedOverDays { get; set; }
    public decimal UsedDays { get; set; }

    public decimal RemainingDays => AnnualAllowanceDays + CarriedOverDays - UsedDays;
}
