using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Infrastructure.Search;

public sealed class SearchService : ISearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IMetadataStore _metadataStore;

    public SearchService(
        IEmbeddingService embeddingService,
        IVectorSearchService vectorSearchService,
        IMetadataStore metadataStore)
    {
        _embeddingService = embeddingService;
        _vectorSearchService = vectorSearchService;
        _metadataStore = metadataStore;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        var queryVector = _embeddingService.GenerateEmbedding(command.Query);
        var queryTokens = Tokenize(command.Query);

        var vectorHits = await _vectorSearchService.SearchAsync(
            queryVector,
            command.RepositoryName,
            command.BranchName,
            topK: command.MaxResults * 3, // fetch more, then re-rank
            cancellationToken);

        var results = new List<SearchResult>();

        foreach (var hit in vectorHits)
        {
            if (!hit.Metadata.TryGetValue("chunkId", out var chunkIdStr)) continue;
            if (!Guid.TryParse(chunkIdStr, out var chunkId)) continue;

            var chunk = await _metadataStore.GetChunkByIdAsync(chunkId, cancellationToken);
            if (chunk is null) continue;

            // Verify document is not deleted
            var doc = await _metadataStore.GetDocumentByPathAsync(
                command.RepositoryName, command.BranchName, chunk.FilePath, cancellationToken);
            if (doc is null || doc.IsDeleted) continue;

            // Hybrid scoring: vector + keyword + symbol + path relevance
            double keywordScore = KeywordOverlap(queryTokens, Tokenize(chunk.Text));
            double symbolScore = SymbolNameScore(queryTokens, chunk.SymbolName);
            double pathScore = PathRelevanceScore(queryTokens, chunk.FilePath);
            double chunkTypeBoost = chunk.ChunkType is Domain.Enums.ChunkType.Method or Domain.Enums.ChunkType.Class
                or Domain.Enums.ChunkType.Interface ? 1.1 : 1.0;

            double hybridScore = (hit.Score * 0.5 + keywordScore * 0.3 + symbolScore * 0.15 + pathScore * 0.05) * chunkTypeBoost;

            results.Add(new SearchResult
            {
                ChunkId = chunkId,
                FilePath = chunk.FilePath,
                RepositoryName = chunk.RepositoryName,
                BranchName = chunk.BranchName,
                SymbolName = chunk.SymbolName,
                Language = chunk.Language,
                StartLine = chunk.StartLine,
                EndLine = chunk.EndLine,
                Text = chunk.Text,
                Score = hybridScore,
                MatchReason = BuildMatchReason(hit.Score, keywordScore, symbolScore, chunk.SymbolName)
            });
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(command.MaxResults)
            .ToList();
    }

    private static HashSet<string> Tokenize(string text) =>
        new(
            text.ToLowerInvariant()
                .Split([' ', '\t', '\n', '\r', '.', ',', '(', ')', '{', '}', '[', ']', '<', '>', '/', '\\', '"', '\'', ';', ':', '-', '_'],
                    StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.Length >= 2),
            StringComparer.OrdinalIgnoreCase);

    private static double KeywordOverlap(HashSet<string> queryTokens, HashSet<string> chunkTokens)
    {
        if (queryTokens.Count == 0) return 0;
        int matches = queryTokens.Count(t => chunkTokens.Contains(t));
        return (double)matches / queryTokens.Count;
    }

    private static double SymbolNameScore(HashSet<string> queryTokens, string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName) || queryTokens.Count == 0) return 0;
        var symbolTokens = Tokenize(symbolName);
        int matches = queryTokens.Count(t => symbolTokens.Contains(t));
        return Math.Min(1.0, (double)matches / queryTokens.Count);
    }

    private static double PathRelevanceScore(HashSet<string> queryTokens, string filePath)
    {
        if (queryTokens.Count == 0) return 0;
        var pathTokens = Tokenize(filePath);
        int matches = queryTokens.Count(t => pathTokens.Contains(t));
        return Math.Min(1.0, (double)matches / queryTokens.Count);
    }

    private static string BuildMatchReason(double vectorScore, double keywordScore, double symbolScore, string symbolName)
    {
        var parts = new List<string>();
        if (vectorScore > 0.6) parts.Add("high semantic similarity");
        else if (vectorScore > 0.3) parts.Add("moderate semantic similarity");
        if (keywordScore > 0.5) parts.Add("strong keyword overlap");
        else if (keywordScore > 0.2) parts.Add("keyword overlap");
        if (symbolScore > 0.3 && !string.IsNullOrEmpty(symbolName)) parts.Add($"symbol match ({symbolName})");
        return parts.Count > 0 ? string.Join(", ", parts) : "weak match";
    }
}
