namespace RepositoryIntelligence.Domain.Models;

public sealed class SearchCodeCommand
{
    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "main";
    public string Query { get; set; } = "";
    public int MaxResults { get; set; } = 20;
}
