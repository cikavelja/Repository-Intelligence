namespace RepositoryIntelligence.Domain.Models;

public sealed class RepositoryDocument
{
    public Guid Id { get; set; }

    public string RepositoryName { get; set; } = "";
    public string BranchName { get; set; } = "";

    public string FilePath { get; set; } = "";
    public string AbsolutePath { get; set; } = "";
    public string Language { get; set; } = "";

    public string ContentHash { get; set; } = "";
    public long FileSizeBytes { get; set; }

    public DateTime LastModifiedUtc { get; set; }
    public DateTime IndexedAtUtc { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}
