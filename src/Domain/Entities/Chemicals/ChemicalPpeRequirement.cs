using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Chemicals;

public class ChemicalPpeRequirement : BaseEntity
{
    public Guid ChemicalId { get; set; }
    public string Requirement { get; set; } = string.Empty;
    public string? IconCode { get; set; }

    // Navigation
    public Chemical Chemical { get; set; } = null!;
}
