using RepositoryIntelligence.Domain.Enums;

namespace RepositoryIntelligence.Domain.Models;

public sealed class CodeDependency
{
    public Guid Id { get; set; }

    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";

    public string SourceFilePath { get; set; } = "";
    public string SourceSymbol { get; set; } = "";

    public string TargetFilePath { get; set; } = "";
    public string TargetSymbol { get; set; } = "";

    public DependencyType DependencyType { get; set; }
}
