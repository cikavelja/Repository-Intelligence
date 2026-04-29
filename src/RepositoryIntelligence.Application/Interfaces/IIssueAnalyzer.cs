using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Application.Interfaces;

public interface IIssueAnalyzer
{
    Task<IssueAnalysisResult> AnalyzeAsync(
        AnalyzeIssueCommand command,
        CancellationToken cancellationToken = default);
}
