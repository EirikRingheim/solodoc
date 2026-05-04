using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Documents;

public class Document : TenantScopedEntity
{
    public Guid FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileKey { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }
    public string? Category { get; set; }
    public UploadPermission Visibility { get; set; } = UploadPermission.Everyone;

    // Navigation
    public DocumentFolder Folder { get; set; } = null!;
}
