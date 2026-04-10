namespace Solodoc.Shared.TaskGroups;

public record TaskGroupListItemDto(
    Guid Id,
    string Name,
    string? Description);

public record TaskGroupDetailDto(
    Guid Id,
    string Name,
    string? Description,
    List<string> Roles);

public record CreateTaskGroupRequest(
    string Name,
    string? Description);
