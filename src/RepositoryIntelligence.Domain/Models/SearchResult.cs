namespace RepositoryIntelligence.Domain.Models;

public sealed class SearchResult
{
    public Guid ChunkId { get; set; }
    public string FilePath { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";
    public string SymbolName { get; set; } = "";
    public string Language { get; set; } = "";
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Text { get; set; } = "";
    public double Score { get; set; }
    public string MatchReason { get; set; } = "";
}
