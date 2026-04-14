using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Projects;

public class Project : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planlagt;
    public Guid? CustomerId { get; set; }
    public string? ClientName { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? QrCodeSlug { get; set; }

    // Hierarchy — one level only (sub-projects cannot have children)
    public Guid? ParentProjectId { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
    public Project? ParentProject { get; set; }
    public ICollection<Project> SubProjects { get; set; } = [];
    public ICollection<ProjectMembership> Memberships { get; set; } = [];
}
