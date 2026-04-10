using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class PublicHoliday : BaseEntity
{
    public Guid? TenantId { get; set; }          // null = system-wide default
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsHalfDay { get; set; }
    public TimeOnly? HalfDayCutoff { get; set; }
    public decimal RatePercent { get; set; } = 100; // Default 100% for most holidays
    public bool IsEnabled { get; set; } = true;
}
