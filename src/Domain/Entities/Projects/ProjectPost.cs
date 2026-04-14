using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Projects;

public class ProjectPost : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? PhotoFileKey { get; set; }
    public bool IsPinned { get; set; }

    // Navigation
    public Project Project { get; set; } = null!;
    public ICollection<ProjectPostComment> Comments { get; set; } = [];
}

public class ProjectPostComment : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;

    public ProjectPost Post { get; set; } = null!;
}
