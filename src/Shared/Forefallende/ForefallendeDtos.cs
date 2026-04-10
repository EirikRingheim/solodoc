namespace Solodoc.Shared.Forefallende;

public record ForefallendItemDto(
    Guid Id,
    string Type,
    string Title,
    string? Subtitle,
    DateTimeOffset DueDate,
    string Column);
