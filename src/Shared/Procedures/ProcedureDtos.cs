namespace Solodoc.Shared.Procedures;

public record ProcedureListItemDto(Guid Id, string Name, string? Description, int BlockCount);

public record ProcedureDetailDto(Guid Id, string Name, string? Description, List<ProcedureBlockDto> Blocks);

public record ProcedureBlockDto(Guid Id, string Type, string Content, int SortOrder, string? ImageFileKey, string? Caption);

public record CreateProcedureRequest(string Name, string? Description);
