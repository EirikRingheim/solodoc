namespace Solodoc.Application.Services;

public interface ITranslationService
{
    Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken ct = default);
    Task<List<string>> TranslateBatchAsync(List<string> texts, string sourceLanguage, string targetLanguage, CancellationToken ct = default);
}
