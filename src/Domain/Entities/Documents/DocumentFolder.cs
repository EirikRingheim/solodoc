using Solodoc.Domain.Common;
using Solodoc.Domain.Enums;

namespace Solodoc.Domain.Entities.Documents;

public class DocumentFolder : TenantScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentFolderId { get; set; }
    public Guid? ProjectId { get; set; }
    public UploadPermission UploadPermission { get; set; } = UploadPermission.AdminAndProjectLeader;
    public bool IsSystemFolder { get; set; }

    // Navigation
    public DocumentFolder? ParentFolder { get; set; }
    public ICollection<DocumentFolder> SubFolders { get; set; } = [];
    public ICollection<Document> Documents { get; set; } = [];
}
