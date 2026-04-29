using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Application.Interfaces;

public interface IRepositoryIndexer
{
    Task<IndexingSummary> IndexRepositoryAsync(
        IndexRepositoryCommand command,
        CancellationToken cancellationToken = default);
}
