using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Chemicals;

public class ChemicalGhsPictogram : BaseEntity
{
    public Guid ChemicalId { get; set; }
    public string PictogramCode { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public Chemical Chemical { get; set; } = null!;
}
