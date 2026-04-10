using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Equipment;

public class Equipment : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? SerialNumber { get; set; }
    public int? Year { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<EquipmentMaintenance> MaintenanceRecords { get; set; } = [];
    public ICollection<EquipmentInspection> Inspections { get; set; } = [];
    public ICollection<EquipmentProjectAssignment> ProjectAssignments { get; set; } = [];
}
