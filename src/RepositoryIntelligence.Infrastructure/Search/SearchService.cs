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
        var queryTokens = TextTokenizer.Tokenize(command.Query);

        var vectorHits = await _vectorSearchService.SearchAsync(
            queryVector,
            command.RepositoryName,
            command.BranchName,
            topK: command.MaxResults * 3,
            cancellationToken);

        // Collect chunk IDs from hits
        var chunkIds = vectorHits
            .Where(h => h.Metadata.TryGetValue("chunkId", out _))
            .Select(h => Guid.TryParse(h.Metadata["chunkId"], out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        // Batch fetch chunks and documents (eliminates N+1 queries)
        var chunks = await _metadataStore.GetChunksByIdsAsync(chunkIds, cancellationToken);
        var filePaths = chunks.Select(c => c.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var documents = await _metadataStore.GetDocumentsByPathsAsync(
            command.RepositoryName, command.BranchName, filePaths, cancellationToken);
        var docByPath = documents.ToDictionary(d => d.FilePath, StringComparer.OrdinalIgnoreCase);
        var hitScoreById = vectorHits
            .Where(h => h.Metadata.TryGetValue("chunkId", out _))
            .ToDictionary(h => h.Metadata["chunkId"], h => h.Score);

        var results = new List<SearchResult>();

        foreach (var chunk in chunks)
        {
            // Skip chunks belonging to deleted documents
            if (!docByPath.TryGetValue(chunk.FilePath, out var doc) || doc.IsDeleted)
                continue;

            var vectorScore = hitScoreById.TryGetValue(chunk.Id.ToString(), out var vs) ? vs : 0;
            double keywordScore = KeywordOverlap(queryTokens, TextTokenizer.Tokenize(chunk.Text));
            double symbolScore = SymbolNameScore(queryTokens, chunk.SymbolName);
            double pathScore = PathRelevanceScore(queryTokens, chunk.FilePath);
            double chunkTypeBoost = chunk.ChunkType is Domain.Enums.ChunkType.Method or Domain.Enums.ChunkType.Class
                or Domain.Enums.ChunkType.Interface ? 1.1 : 1.0;

            double hybridScore = (vectorScore * 0.5 + keywordScore * 0.3 + symbolScore * 0.15 + pathScore * 0.05) * chunkTypeBoost;

            results.Add(new SearchResult
            {
                ChunkId = chunk.Id,
                FilePath = chunk.FilePath,
                RepositoryName = chunk.RepositoryName,
                BranchName = chunk.BranchName,
                SymbolName = chunk.SymbolName,
                Language = chunk.Language,
                StartLine = chunk.StartLine,
                EndLine = chunk.EndLine,
                Text = chunk.Text,
                Score = hybridScore,
                MatchReason = BuildMatchReason(vectorScore, keywordScore, symbolScore, chunk.SymbolName)
            });
        }

        return results
            .OrderByDescending(r => r.Score)
            .Take(command.MaxResults)
            .ToList();
    }

    private static double KeywordOverlap(HashSet<string> queryTokens, HashSet<string> chunkTokens)
    {
        if (queryTokens.Count == 0) return 0;
        int matches = queryTokens.Count(t => chunkTokens.Contains(t));
        return (double)matches / queryTokens.Count;
    }

    private static double SymbolNameScore(HashSet<string> queryTokens, string symbolName)
    {
        if (string.IsNullOrEmpty(symbolName) || queryTokens.Count == 0) return 0;
        var symbolTokens = TextTokenizer.Tokenize(symbolName);
        int matches = queryTokens.Count(t => symbolTokens.Contains(t));
        return Math.Min(1.0, (double)matches / queryTokens.Count);
    }

    private static double PathRelevanceScore(HashSet<string> queryTokens, string filePath)
    {
        if (queryTokens.Count == 0) return 0;
        var pathTokens = TextTokenizer.Tokenize(filePath);
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
