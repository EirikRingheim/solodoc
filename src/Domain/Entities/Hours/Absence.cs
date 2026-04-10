using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Hours;

public class Absence : TenantScopedEntity
{
    public Guid PersonId { get; set; }
    public AbsenceType Type { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Hours { get; set; } // Total hours for the period
    public string? Notes { get; set; }
    public AbsenceStatus Status { get; set; } = AbsenceStatus.Registered;
}
