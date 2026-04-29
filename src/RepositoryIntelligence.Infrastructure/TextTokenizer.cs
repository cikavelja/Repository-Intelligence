namespace RepositoryIntelligence.Infrastructure;

/// <summary>
/// Shared tokenizer used by the embedding service, search service, and issue analyzer
/// to ensure consistent tokenization across all text-processing components.
/// </summary>
internal static class TextTokenizer
{
    private static readonly char[] Separators =
        [' ', '\t', '\n', '\r', '.', ',', '(', ')', '{', '}', '[', ']', '<', '>', '/', '\\', '"', '\'', ';', ':', '-', '_'];

    /// <summary>Returns a case-insensitive HashSet of tokens (length >= 2).</summary>
    public static HashSet<string> Tokenize(string text) =>
        new(
            text.ToLowerInvariant()
                .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 2),
            StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns tokens as IEnumerable; avoids HashSet allocation for streaming use.</summary>
    public static IEnumerable<string> TokenizeEnumerable(string text) =>
        text.ToLowerInvariant()
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2);
}
