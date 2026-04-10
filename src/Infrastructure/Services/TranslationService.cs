using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Solodoc.Application.Services;
using Solodoc.Domain.Entities.Translations;
using Solodoc.Infrastructure.Persistence;

namespace Solodoc.Infrastructure.Services;

public class TranslationService(
    IConfiguration configuration,
    SolodocDbContext db,
    ILogger<TranslationService> logger,
    IHttpClientFactory httpClientFactory) : ITranslationService
{
    private readonly string? _apiKey = configuration["DeepL:ApiKey"];

    // Construction domain glossary for key Norwegian terms
    private static readonly Dictionary<string, Dictionary<string, string>> Glossary = new()
    {
        ["EN"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["avvik"] = "deviation",
            ["sjekkliste"] = "checklist",
            ["vernerunde"] = "safety round",
            ["stoffkartotek"] = "chemical register",
            ["sikker jobbanalyse"] = "safe job analysis",
            ["HMS"] = "HSE",
            ["prosjektleder"] = "project leader",
            ["underentreprenør"] = "subcontractor",
            ["oppdrag"] = "job",
            ["prosjekt"] = "project",
            ["sertifikat"] = "certification",
            ["prosedyre"] = "procedure",
            ["skjema"] = "schema",
            ["mannskap"] = "crew",
            ["innsjekking"] = "check-in"
        },
        ["PL"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["avvik"] = "odchylenie",
            ["sjekkliste"] = "lista kontrolna",
            ["HMS"] = "BHP"
        },
        ["ES"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["avvik"] = "desviacion",
            ["sjekkliste"] = "lista de verificacion",
            ["HMS"] = "SST"
        }
    };

    public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var sourceLang = sourceLanguage.ToUpperInvariant();
        var targetLang = targetLanguage.ToUpperInvariant();

        // Check cache first
        var cached = await db.Translations
            .FirstOrDefaultAsync(t =>
                t.SourceText == text &&
                t.SourceLanguageCode == sourceLang &&
                t.LanguageCode == targetLang, ct);

        if (cached is not null)
            return cached.TranslatedText;

        if (string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("DeepL API key not configured. Returning original text");
            return text;
        }

        var httpClient = httpClientFactory.CreateClient("DeepL");

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("text", text),
            new KeyValuePair<string, string>("source_lang", sourceLang),
            new KeyValuePair<string, string>("target_lang", targetLang)
        });

        try
        {
            var response = await httpClient.PostAsync("https://api-free.deepl.com/v2/translate", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("DeepL API call failed with status {Status}", response.StatusCode);
                return text;
            }

            var result = await response.Content.ReadFromJsonAsync<DeepLResponse>(ct);
            var translated = result?.Translations?.FirstOrDefault()?.Text ?? text;

            // Cache the translation
            db.Translations.Add(new Translation
            {
                EntityType = "DirectTranslation",
                EntityId = Guid.Empty,
                FieldName = "text",
                SourceText = text,
                SourceLanguageCode = sourceLang,
                LanguageCode = targetLang,
                TranslatedText = translated
            });
            await db.SaveChangesAsync(ct);

            return translated;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to call DeepL API. Returning original text");
            return text;
        }
    }

    public async Task<List<string>> TranslateBatchAsync(List<string> texts, string sourceLanguage, string targetLanguage, CancellationToken ct = default)
    {
        var results = new List<string>(texts.Count);
        foreach (var text in texts)
        {
            results.Add(await TranslateTextAsync(text, sourceLanguage, targetLanguage, ct));
        }
        return results;
    }

    /// <summary>
    /// Gets the glossary term for a given word in the target language, if available.
    /// </summary>
    public static string? GetGlossaryTerm(string word, string targetLanguage)
    {
        var lang = targetLanguage.ToUpperInvariant();
        if (Glossary.TryGetValue(lang, out var terms) && terms.TryGetValue(word, out var translation))
            return translation;
        return null;
    }

    private record DeepLResponse(List<DeepLTranslation>? Translations);
    private record DeepLTranslation(string Text);
}
