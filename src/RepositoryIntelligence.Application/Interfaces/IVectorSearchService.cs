namespace RepositoryIntelligence.Application.Interfaces;

public interface IVectorSearchService
{
    Task UpsertAsync(string id, float[] vector, IDictionary<string, string> metadata, CancellationToken cancellationToken = default);

    Task DeleteByFilePathAsync(string repositoryName, string branchName, string filePath, CancellationToken cancellationToken = default);

    Task DeleteByDocumentIdAsync(Guid repositoryDocumentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(
        float[] queryVector,
        string repositoryName,
        string branchName,
        int topK = 20,
        CancellationToken cancellationToken = default);
}
