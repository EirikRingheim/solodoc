using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Chemicals;

public class Chemical : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? ProductNumber { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<ChemicalSds> SdsDocuments { get; set; } = [];
    public ICollection<ChemicalGhsPictogram> GhsPictograms { get; set; } = [];
    public ICollection<ChemicalPpeRequirement> PpeRequirements { get; set; } = [];
}
