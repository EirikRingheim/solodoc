using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Projects;

public class ProjectMembership : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid PersonId { get; set; }
    public string? Role { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
}
