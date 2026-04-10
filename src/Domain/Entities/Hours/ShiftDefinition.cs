using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hours;

public class ShiftDefinition : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;  // "Dagskift", "Nattskift", "Fri"
    public string Color { get; set; } = "#4361EE";     // Hex color for planner display
    public bool IsWorkDay { get; set; } = true;         // false = off day
    public TimeOnly? StartTime { get; set; }            // null for off days
    public TimeOnly? EndTime { get; set; }              // null for off days
    public int BreakMinutes { get; set; }
    public decimal NormalHours { get; set; }             // Auto-calculated or manual
    public Guid? ProjectId { get; set; }                 // Optional: tie shift to a project
    public bool IsActive { get; set; } = true;
}
