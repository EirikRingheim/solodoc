namespace Solodoc.Shared.Documents;

public record DocumentFolderDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentFolderId,
    Guid? ProjectId,
    string UploadPermission,
    bool IsSystemFolder,
    int DocumentCount,
    int SubFolderCount);

public record DocumentDto(
    Guid Id,
    string Name,
    string? Description,
    string FileKey,
    long FileSize,
    string ContentType,
    string UploadedByName,
    string? Category,
    DateTimeOffset CreatedAt);

public record CreateFolderRequest(
    string Name,
    string? Description,
    Guid? ParentFolderId,
    Guid? ProjectId,
    string? UploadPermission);

public record UploadDocumentRequest(
    Guid FolderId,
    string? Name,
    string? Description,
    string? Category);
