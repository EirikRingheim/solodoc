namespace Solodoc.Shared.Procedures;

public record ProcedureListItemDto(Guid Id, string Name, string? Description, string? Category, int BlockCount);

public record ProcedureDetailDto(Guid Id, string Name, string? Description, string? Category, bool IsPublished, List<ProcedureBlockDto> Blocks);

public record ProcedureBlockDto(Guid Id, string Type, string Content, int SortOrder, string? ImageFileKey, string? Caption);

public record CreateProcedureRequest(string Name, string? Description, string? Category = null);
