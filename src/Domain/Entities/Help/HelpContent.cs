using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Help;

public class HelpContent : BaseEntity
{
    public string PageIdentifier { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? RoleScope { get; set; }
    public string Language { get; set; } = "nb";
}
