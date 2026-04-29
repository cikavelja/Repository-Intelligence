using RepositoryIntelligence.Domain.Enums;

namespace RepositoryIntelligence.Domain.Models;

public sealed class CodeSymbol
{
    public Guid Id { get; set; }
    public Guid RepositoryDocumentId { get; set; }

    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";

    public string FilePath { get; set; } = "";
    public string SymbolName { get; set; } = "";

    public SymbolType SymbolType { get; set; }

    public string Namespace { get; set; } = "";

    public int StartLine { get; set; }
    public int EndLine { get; set; }
}
