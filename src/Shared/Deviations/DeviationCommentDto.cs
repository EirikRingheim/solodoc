namespace Solodoc.Shared.Deviations;

public record DeviationCommentDto(Guid Id, string AuthorName, string Text, DateTimeOffset PostedAt);
