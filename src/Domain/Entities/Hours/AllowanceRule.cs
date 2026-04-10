using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Hours;

public class AllowanceRule : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public AllowanceType Type { get; set; }
    public AllowanceAmountType AmountType { get; set; }
    public decimal Amount { get; set; }
    public TimeOnly? TimeRangeStart { get; set; }
    public TimeOnly? TimeRangeEnd { get; set; }
    public string? ApplicableDays { get; set; } // JSON array of DayOfWeek values
    public bool IsActive { get; set; } = true;
    public DateOnly? ActiveFrom { get; set; }
    public int Priority { get; set; }
    public Guid? ShiftDefinitionId { get; set; }         // null = applies to all shifts
    public bool StacksWithOvertime { get; set; } = true; // Whether this allowance stacks with overtime rates
}
