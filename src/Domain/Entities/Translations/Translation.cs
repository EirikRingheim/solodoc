using Solodoc.Domain.Common;

namespace Solodoc.Domain.Entities.Translations;

public class Translation : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public string SourceLanguageCode { get; set; } = "nb";
    public string SourceText { get; set; } = string.Empty;
}
