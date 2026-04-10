using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Projects;

public class Job : TenantScopedEntity
{
    public string Description { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Active;
    public Guid? CustomerId { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? PromotedToProjectId { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
    public ICollection<JobPartsItem> PartsItems { get; set; } = [];
}
