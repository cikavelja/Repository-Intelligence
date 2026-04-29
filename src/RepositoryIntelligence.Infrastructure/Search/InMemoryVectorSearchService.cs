using RepositoryIntelligence.Application.Interfaces;

namespace RepositoryIntelligence.Infrastructure.Search;

/// <summary>
/// In-memory vector search service using cosine similarity.
/// </summary>
public sealed class InMemoryVectorSearchService : IVectorSearchService
{
    private readonly record struct VectorEntry(string Id, float[] Vector, IDictionary<string, string> Metadata);

    private readonly List<VectorEntry> _entries = [];
    private readonly Lock _lock = new();

    public Task UpsertAsync(string id, float[] vector, IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var idx = _entries.FindIndex(e => e.Id == id);
            if (idx >= 0)
                _entries[idx] = new VectorEntry(id, vector, metadata);
            else
                _entries.Add(new VectorEntry(id, vector, metadata));
        }
        return Task.CompletedTask;
    }

    public Task DeleteByFilePathAsync(string repositoryName, string branchName, string filePath, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _entries.RemoveAll(e =>
                e.Metadata.TryGetValue("repositoryName", out var rn) && rn == repositoryName &&
                e.Metadata.TryGetValue("branchName", out var bn) && bn == branchName &&
                e.Metadata.TryGetValue("filePath", out var fp) && fp == filePath);
        }
        return Task.CompletedTask;
    }

    public Task DeleteByDocumentIdAsync(Guid repositoryDocumentId, CancellationToken cancellationToken = default)
    {
        var idStr = repositoryDocumentId.ToString();
        lock (_lock)
        {
            _entries.RemoveAll(e =>
                e.Metadata.TryGetValue("repositoryDocumentId", out var did) && did == idStr);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        float[] queryVector,
        string repositoryName,
        string branchName,
        int topK = 20,
        CancellationToken cancellationToken = default)
    {
        List<VectorEntry> candidates;
        lock (_lock)
        {
            candidates = _entries
                .Where(e =>
                    e.Metadata.TryGetValue("repositoryName", out var rn) && rn == repositoryName &&
                    e.Metadata.TryGetValue("branchName", out var bn) && bn == branchName)
                .ToList();
        }

        var results = candidates
            .Select(e => new VectorSearchHit
            {
                Id = e.Id,
                Score = CosineSimilarity(queryVector, e.Vector),
                Metadata = e.Metadata
            })
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchHit>>(results);
    }

    public static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        int len = Math.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
