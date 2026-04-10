using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hms;

public class SafetyRoundSchedule : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ProjectId { get; set; }
    public Guid? ChecklistTemplateId { get; set; }
    public int FrequencyWeeks { get; set; }
    public DateOnly NextDue { get; set; }
    public bool IsActive { get; set; } = true;
}
