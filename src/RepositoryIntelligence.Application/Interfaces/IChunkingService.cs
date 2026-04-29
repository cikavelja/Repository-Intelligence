using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Application.Interfaces;

public interface IChunkingService
{
    IReadOnlyList<CodeChunk> ChunkText(
        string content,
        string filePath,
        string language,
        string repositoryName,
        string branchName,
        Guid repositoryDocumentId);
}
