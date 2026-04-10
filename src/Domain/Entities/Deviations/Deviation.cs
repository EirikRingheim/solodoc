using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Deviations;

public class Deviation : TenantScopedEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DeviationStatus Status { get; set; } = DeviationStatus.Open;
    public DeviationSeverity Severity { get; set; } = DeviationSeverity.Medium;
    public Guid ReportedById { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? LocationId { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public Guid? ClosedById { get; set; }

    // Phase 2.2 fields
    public DeviationType? Type { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CorrectiveAction { get; set; }
    public DateTimeOffset? CorrectiveActionDeadline { get; set; }
    public DateTimeOffset? CorrectiveActionCompletedAt { get; set; }

    // GPS at report time
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? LocationAccuracy { get; set; }

    // Confidentiality
    public bool IsConfidential { get; set; }

    // Personskade-specific fields
    public string? InjuryDescription { get; set; }
    public string? BodyPart { get; set; }
    public bool? FirstAidGiven { get; set; }
    public bool? HospitalVisit { get; set; }

    // Navigation properties
    public DeviationCategory? Category { get; set; }
    public ICollection<DeviationPhoto> Photos { get; set; } = new List<DeviationPhoto>();
    public ICollection<DeviationComment> Comments { get; set; } = new List<DeviationComment>();
    public ICollection<RelatedDeviation> RelatedDeviations { get; set; } = new List<RelatedDeviation>();
    public ICollection<DeviationVisibility> VisibleTo { get; set; } = new List<DeviationVisibility>();
}
