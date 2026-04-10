namespace Solodoc.Shared.Search;

public record SearchResultDto(
    string Type,
    Guid Id,
    string Title,
    string? Subtitle,
    string? Highlight);

public record SearchResponse(
    List<SearchResultDto> Results,
    int TotalCount);
