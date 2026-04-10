namespace Solodoc.Infrastructure.Services;

public static class AutoTagger
{
    private static readonly HashSet<string> NorwegianStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "og", "for", "i", "av", "er", "som", "den", "det", "en", "et",
        "til", "på", "med", "fra", "om", "har", "kan", "skal", "ved",
        "til", "ikke", "var", "vil", "ble", "bli", "blitt", "bare",
        "da", "de", "denne", "der", "disse", "du", "etter", "han",
        "hun", "hva", "hver", "hvilken", "hvis", "jeg", "mange",
        "meg", "mellom", "men", "min", "mot", "mye", "nå", "når",
        "noe", "noen", "opp", "over", "seg", "sin", "slik", "så",
        "under", "ut", "ved", "vi", "vår", "være"
    };

    public static string ExtractTags(string name, IEnumerable<string> itemLabels)
    {
        var words = new List<string>();
        words.AddRange(Tokenize(name));
        foreach (var label in itemLabels)
            words.AddRange(Tokenize(label));

        var tags = words
            .Where(w => w.Length > 2 && !NorwegianStopWords.Contains(w))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10);

        return string.Join(",", tags);
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return text.ToLowerInvariant()
            .Split(new[] { ' ', '-', '/', '(', ')', ',', '.', ':' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
