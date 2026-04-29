namespace RepositoryIntelligence.Domain.Models;

public sealed class IndexingSummary
{
    public int AddedFiles { get; set; }
    public int UpdatedFiles { get; set; }
    public int DeletedFiles { get; set; }
    public int UnchangedFiles { get; set; }

    public int AddedChunks { get; set; }
    public int DeletedChunks { get; set; }

    public TimeSpan Duration { get; set; }
}
