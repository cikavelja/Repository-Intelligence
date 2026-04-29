using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Application.Interfaces;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchCodeCommand command,
        CancellationToken cancellationToken = default);
}
