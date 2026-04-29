using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Application.Interfaces;

public interface IMetadataStore
{
    // Repository documents
    Task UpsertDocumentAsync(RepositoryDocument document, CancellationToken cancellationToken = default);
    Task<RepositoryDocument?> GetDocumentByPathAsync(string repositoryName, string branchName, string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RepositoryDocument>> GetAllDocumentsAsync(string repositoryName, string branchName, CancellationToken cancellationToken = default);
    Task MarkDocumentDeletedAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

    // Chunks
    Task AddChunksAsync(IReadOnlyList<CodeChunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeChunk>> GetAllChunksAsync(string repositoryName, string branchName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeChunk>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeChunk>> GetChunksByFilePathAsync(string repositoryName, string branchName, string filePath, CancellationToken cancellationToken = default);
    Task DeleteChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<CodeChunk?> GetChunkByIdAsync(Guid chunkId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeChunk>> GetChunksByIdsAsync(IReadOnlyList<Guid> chunkIds, CancellationToken cancellationToken = default);

    // Documents (batch)
    Task<IReadOnlyList<RepositoryDocument>> GetDocumentsByPathsAsync(string repositoryName, string branchName, IReadOnlyList<string> filePaths, CancellationToken cancellationToken = default);

    // Symbols
    Task AddSymbolsAsync(IReadOnlyList<CodeSymbol> symbols, CancellationToken cancellationToken = default);
    Task DeleteSymbolsByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    // Dependencies
    Task AddDependenciesAsync(IReadOnlyList<CodeDependency> dependencies, CancellationToken cancellationToken = default);
    Task DeleteDependenciesBySourceFileAsync(string repositoryName, string branchName, string filePath, CancellationToken cancellationToken = default);

    // Repository-level operations
    Task ClearRepositoryAsync(string repositoryName, string branchName, CancellationToken cancellationToken = default);
    Task<IndexingSummary> GetIndexingSummaryAsync(string repositoryName, string branchName, CancellationToken cancellationToken = default);

    Task InitializeAsync(CancellationToken cancellationToken = default);
}
