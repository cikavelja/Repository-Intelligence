namespace RepositoryIntelligence.Domain.Models;

public sealed class AnalyzeIssueCommand
{
    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "main";
    public string Issue { get; set; } = "";
    public int MaxRecommendations { get; set; } = 10;
}
