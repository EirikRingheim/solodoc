using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Locations;

public class Location : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;        // "Hovedkontor", "Lager Arna", "Verksted"
    public string? Address { get; set; }
    public string? Description { get; set; }
    public string LocationType { get; set; } = "Annet";      // "Kontor", "Lager", "Verksted", "Gard", "Annet"
    public string? QrCodeSlug { get; set; }
    public bool IsActive { get; set; } = true;
}
