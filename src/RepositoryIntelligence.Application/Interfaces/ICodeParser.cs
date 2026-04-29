using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Application.Interfaces;

public interface ICodeParser
{
    bool CanParse(string filePath);

    Task<ParsedFile> ParseAsync(
        string filePath,
        string content,
        string repositoryName,
        string branchName,
        Guid repositoryDocumentId,
        CancellationToken cancellationToken = default);
}
