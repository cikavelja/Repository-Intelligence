namespace RepositoryIntelligence.Application.Interfaces;

public sealed class VectorSearchHit
{
    public string Id { get; set; } = "";
    public double Score { get; set; }
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
