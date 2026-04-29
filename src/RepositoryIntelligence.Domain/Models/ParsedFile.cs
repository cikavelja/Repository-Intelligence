namespace RepositoryIntelligence.Domain.Models;

public sealed class ParsedFile
{
    public string FilePath { get; set; } = "";
    public string Language { get; set; } = "";
    public IReadOnlyList<CodeChunk> Chunks { get; set; } = [];
    public IReadOnlyList<CodeSymbol> Symbols { get; set; } = [];
    public IReadOnlyList<CodeDependency> Dependencies { get; set; } = [];
}
