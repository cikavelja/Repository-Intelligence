using RepositoryIntelligence.Domain.Enums;

namespace RepositoryIntelligence.Domain.Models;

public sealed class CodeChunk
{
    public Guid Id { get; set; }
    public Guid RepositoryDocumentId { get; set; }

    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";

    public string FilePath { get; set; } = "";
    public string Language { get; set; } = "";

    public ChunkType ChunkType { get; set; }
    public string SymbolName { get; set; } = "";

    public int StartLine { get; set; }
    public int EndLine { get; set; }

    public string Text { get; set; } = "";
    public string ContentHash { get; set; } = "";

    public string EmbeddingId { get; set; } = "";
}
