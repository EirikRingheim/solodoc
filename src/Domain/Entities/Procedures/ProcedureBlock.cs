using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Procedures;

public class ProcedureBlock : BaseEntity
{
    public Guid ProcedureTemplateId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string? ImageFileKey { get; set; }  // For photo/illustration per block
    public string? Caption { get; set; }        // Caption for images

    // Navigation properties
    public ProcedureTemplate ProcedureTemplate { get; set; } = null!;
}
