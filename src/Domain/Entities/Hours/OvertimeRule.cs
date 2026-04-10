using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Hours;

public class OvertimeRule : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;       // "Vanlig overtid", "Helgovertid sondag"
    public int Priority { get; set; }                       // Lower = higher priority
    public decimal RatePercent { get; set; }                 // e.g., 50 for 50%, 100 for 100%

    // When this rule applies
    public string? ApplicableDays { get; set; }              // JSON: ["Monday","Tuesday",...] or null = all days
    public bool AppliesToRedDays { get; set; }               // Applies on public holidays
    public bool AppliesToSaturday { get; set; }
    public bool AppliesToSunday { get; set; }
    public bool AppliesToWeekdays { get; set; } = true;
    public TimeOnly? TimeRangeStart { get; set; }            // null = all day
    public TimeOnly? TimeRangeEnd { get; set; }

    // Shift-specific
    public Guid? ShiftDefinitionId { get; set; }             // null = applies to all shifts

    public bool IsActive { get; set; } = true;
}
