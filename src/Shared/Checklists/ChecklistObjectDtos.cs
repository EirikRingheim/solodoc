namespace Solodoc.Shared.Checklists;

public record ChecklistObjectDto(
    Guid Id,
    string Name,
    int Number,
    string DisplayName,
    Guid ProjectId,
    int TotalChecklists,
    int CompletedChecklists,
    List<ObjectChecklistStatusDto> Checklists);

public record ObjectChecklistStatusDto(
    Guid InstanceId,
    string TemplateName,
    string Status,
    DateTimeOffset? SubmittedAt);

public record CreateChecklistObjectRequest(
    Guid ProjectId,
    string Name,
    int Number,
    List<Guid> TemplateIds);

public record CopyChecklistObjectRequest(
    Guid SourceObjectId,
    int? NewNumber);
