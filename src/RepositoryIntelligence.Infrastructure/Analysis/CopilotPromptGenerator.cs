using RepositoryIntelligence.Application.Interfaces;
using RepositoryIntelligence.Domain.Models;
using System.Text;

namespace RepositoryIntelligence.Infrastructure.Analysis;

public sealed class CopilotPromptGenerator : ICopilotPromptGenerator
{
    public Task<string> GeneratePromptAsync(
        IssueAnalysisResult analysisResult,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Issue:");
        sb.AppendLine(analysisResult.Issue);
        sb.AppendLine();

        sb.AppendLine("Relevant files:");
        for (int i = 0; i < analysisResult.RecommendedFiles.Count; i++)
        {
            var rec = analysisResult.RecommendedFiles[i];
            sb.AppendLine($"{i + 1}. {rec.FilePath}");
        }
        sb.AppendLine();

        sb.AppendLine("Task:");
        sb.AppendLine($"Investigate and fix the following issue: {analysisResult.Issue}");
        sb.AppendLine();

        sb.AppendLine("Constraints:");
        sb.AppendLine("- Do not modify unrelated files.");
        sb.AppendLine("- Respect Clean Architecture boundaries.");
        sb.AppendLine("- Do not put business logic in controllers.");

        foreach (var rec in analysisResult.RecommendedFiles.Take(5))
        {
            if (!string.IsNullOrEmpty(rec.SuggestedCheck))
                sb.AppendLine($"- {rec.SuggestedCheck}");
        }

        sb.AppendLine("- Add or update tests where needed.");
        sb.AppendLine("- Run dotnet build and dotnet test.");

        return Task.FromResult(sb.ToString());
    }
}
