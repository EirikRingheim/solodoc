using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Help;

public class Feedback : BaseEntity
{
    public Guid? PersonId { get; set; }
    public string? Page { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
