using RepositoryIntelligence.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace RepositoryIntelligence.Infrastructure.Embeddings;

/// <summary>
/// Deterministic local mock embedding service. Produces a 128-dimensional float vector
/// by hashing tokens derived from the input text. No external API required.
/// </summary>
public sealed class LocalHashEmbeddingService : IEmbeddingService
{
    public int Dimensions => 128;

    public float[] GenerateEmbedding(string text)
    {
        var tokens = Tokenize(text);
        var vector = new float[Dimensions];

        foreach (var token in tokens)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            // Spread token hash across all dimensions
            for (int i = 0; i < Dimensions; i++)
            {
                int byteIndex = i % bytes.Length;
                // Map byte to [-1, 1] range
                vector[i] += (bytes[byteIndex] / 127.5f) - 1.0f;
            }
        }

        // Normalize to unit vector
        var magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude > 0)
        {
            for (int i = 0; i < Dimensions; i++)
                vector[i] /= magnitude;
        }

        return vector;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        // Lowercase, split on non-alphanumeric, remove empties
        return text
            .ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', '.', ',', '(', ')', '{', '}', '[', ']', '<', '>', '/', '\\', '"', '\'', ';', ':', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 2);
    }
}
