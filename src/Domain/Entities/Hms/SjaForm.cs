using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Hms;

public class SjaForm : TenantScopedEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ProjectId { get; set; }
    public string Status { get; set; } = "Draft";
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public Guid CreatedById { get; set; }

    // Navigation
    public ICollection<SjaParticipant> Participants { get; set; } = [];
    public ICollection<SjaHazard> Hazards { get; set; } = [];
}
