namespace RepositoryIntelligence.Domain.Models;

public sealed class FileChangeRecommendation
{
    public string FilePath { get; set; } = "";
    public double Confidence { get; set; }
    public string Reason { get; set; } = "";
    public IReadOnlyList<string> RelatedSymbols { get; set; } = [];
    public string SuggestedCheck { get; set; } = "";
}
