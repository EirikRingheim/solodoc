using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Hours;

public class OvertimeBankEntry : TenantScopedEntity
{
    public Guid PersonId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Hours { get; set; } // Positive = credited, negative = used
    public OvertimeBankAction Action { get; set; }
    public string? Description { get; set; }
    public Guid? TimeEntryId { get; set; } // If credited from a specific time entry
}
