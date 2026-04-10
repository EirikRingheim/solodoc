using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Equipment;

public class EquipmentMaintenance : BaseEntity
{
    public Guid EquipmentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public Guid? PerformedById { get; set; }
    public decimal? Cost { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Equipment Equipment { get; set; } = null!;
}
