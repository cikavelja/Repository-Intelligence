using RepositoryIntelligence.Domain.Enums;

namespace RepositoryIntelligence.Domain.Models;

public sealed class IndexRepositoryCommand
{
    public string RepositoryPath { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "main";
    public IndexingMode Mode { get; set; } = IndexingMode.Incremental;
}
