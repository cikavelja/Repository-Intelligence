namespace RepositoryIntelligence.Domain.Models;

public sealed class IssueAnalysisResult
{
    public string Issue { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";
    public IReadOnlyList<FileChangeRecommendation> RecommendedFiles { get; set; } = [];
    public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;
}
