using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Equipment;

public class EquipmentInspection : BaseEntity
{
    public Guid EquipmentId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public Guid? InspectedById { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FileKey { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Equipment Equipment { get; set; } = null!;
}
