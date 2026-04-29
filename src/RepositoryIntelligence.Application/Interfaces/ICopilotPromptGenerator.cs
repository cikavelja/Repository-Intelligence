using RepositoryIntelligence.Domain.Models;

namespace RepositoryIntelligence.Application.Interfaces;

public interface ICopilotPromptGenerator
{
    Task<string> GeneratePromptAsync(
        IssueAnalysisResult analysisResult,
        CancellationToken cancellationToken = default);
}
