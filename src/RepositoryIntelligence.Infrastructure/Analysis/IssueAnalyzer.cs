using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Infrastructure.Analysis;

public sealed class IssueAnalyzer : IIssueAnalyzer
{
    private readonly ISearchService _searchService;
    private readonly IMetadataStore _metadataStore;

    public IssueAnalyzer(ISearchService searchService, IMetadataStore metadataStore)
    {
        _searchService = searchService;
        _metadataStore = metadataStore;
    }

    public async Task<IssueAnalysisResult> AnalyzeAsync(
        AnalyzeIssueCommand command,
        CancellationToken cancellationToken = default)
    {
        var searchCommand = new SearchCodeCommand
        {
            RepositoryName = command.RepositoryName,
            BranchName = command.BranchName,
            Query = command.Issue,
            MaxResults = 60
        };

        var searchResults = await _searchService.SearchAsync(searchCommand, cancellationToken);

        // Group results by file
        var byFile = searchResults
            .GroupBy(r => r.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var issueTokens = Tokenize(command.Issue);

        var recommendations = new List<FileChangeRecommendation>();

        foreach (var (filePath, chunks) in byFile)
        {
            var topChunk = chunks.OrderByDescending(c => c.Score).First();
            var allSymbols = chunks
                .Select(c => c.SymbolName)
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            double fileScore = ComputeFileScore(filePath, chunks, issueTokens);

            var reason = BuildReason(filePath, chunks, allSymbols, issueTokens);
            var suggestedCheck = BuildSuggestedCheck(filePath, allSymbols, issueTokens);

            recommendations.Add(new FileChangeRecommendation
            {
                FilePath = filePath,
                Confidence = Math.Round(Math.Min(1.0, fileScore), 2),
                Reason = reason,
                RelatedSymbols = allSymbols,
                SuggestedCheck = suggestedCheck
            });
        }

        var ranked = recommendations
            .OrderByDescending(r => r.Confidence)
            .Take(command.MaxRecommendations)
            .ToList();

        return new IssueAnalysisResult
        {
            Issue = command.Issue,
            RepositoryName = command.RepositoryName,
            BranchName = command.BranchName,
            RecommendedFiles = ranked
        };
    }

    private static double ComputeFileScore(string filePath, List<SearchResult> chunks, HashSet<string> issueTokens)
    {
        // Average top-3 chunk scores
        double avgScore = chunks.OrderByDescending(c => c.Score).Take(3).Average(c => c.Score);

        // Boost for path relevance
        var pathTokens = Tokenize(filePath);
        int pathMatches = issueTokens.Count(t => pathTokens.Contains(t));
        double pathBoost = pathMatches > 0 ? 1.0 + (pathMatches * 0.05) : 1.0;

        return avgScore * pathBoost;
    }

    private static string BuildReason(string filePath, List<SearchResult> chunks, List<string> symbols, HashSet<string> issueTokens)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var pathTokens = Tokenize(filePath);
        var matchedPathTerms = issueTokens.Where(t => pathTokens.Contains(t)).ToList();

        var parts = new List<string>();

        if (matchedPathTerms.Count > 0)
            parts.Add($"File path matches issue terms: {string.Join(", ", matchedPathTerms)}.");

        if (symbols.Count > 0)
        {
            var relevantSymbols = symbols
                .Where(s => issueTokens.Any(t => s.Contains(t, StringComparison.OrdinalIgnoreCase)))
                .Take(3)
                .ToList();
            if (relevantSymbols.Count > 0)
                parts.Add($"Contains symbols related to issue: {string.Join(", ", relevantSymbols)}.");
        }

        if (chunks.Count > 1)
            parts.Add($"Multiple code sections ({chunks.Count}) matched the issue query.");

        return parts.Count > 0
            ? string.Join(" ", parts)
            : $"This file scored highly for semantic similarity to the issue query.";
    }

    private static string BuildSuggestedCheck(string filePath, List<string> symbols, HashSet<string> issueTokens)
    {
        if (filePath.Contains("test", StringComparison.OrdinalIgnoreCase))
            return "Review test coverage for the affected scenario. Add or update tests.";

        if (filePath.Contains("handler", StringComparison.OrdinalIgnoreCase))
            return "Verify the command/event handler completes all required side effects.";

        if (filePath.Contains("publisher", StringComparison.OrdinalIgnoreCase) ||
            filePath.Contains("servicebus", StringComparison.OrdinalIgnoreCase) ||
            filePath.Contains("messaging", StringComparison.OrdinalIgnoreCase))
            return "Verify event publication: queue/topic name, serialization, and error handling.";

        if (filePath.Contains("listener", StringComparison.OrdinalIgnoreCase) ||
            filePath.Contains("consumer", StringComparison.OrdinalIgnoreCase))
            return "Verify consumer registration, subscription, and message deserialization.";

        if (filePath.Contains("event", StringComparison.OrdinalIgnoreCase))
            return "Verify the event contract matches the publisher and consumer expectations.";

        return "Review this file for logic related to the described issue.";
    }

    private static HashSet<string> Tokenize(string text) =>
        new(
            text.ToLowerInvariant()
                .Split([' ', '\t', '\n', '\r', '.', ',', '(', ')', '{', '}', '[', ']', '<', '>', '/', '\\', '"', '\'', ';', ':', '-', '_'],
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 2),
            StringComparer.OrdinalIgnoreCase);
}
