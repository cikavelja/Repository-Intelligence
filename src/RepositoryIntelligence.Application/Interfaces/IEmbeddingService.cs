namespace RepositoryIntelligence.Application.Interfaces;

public interface IEmbeddingService
{
    /// <summary>
    /// Returns a deterministic embedding vector for the given text.
    /// </summary>
    float[] GenerateEmbedding(string text);

    int Dimensions { get; }
}
